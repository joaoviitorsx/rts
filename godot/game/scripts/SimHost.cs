using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Ironvale.Sim;
using Ironvale.Sim.Commands;
using Ironvale.Sim.Content;
using Ironvale.Sim.Events;
using Ironvale.Sim.Save;
using Ironvale.Sim.Time;
using Ironvale.Game.UI;

namespace Ironvale.Game;

/// <summary>
/// Owns the <see cref="World"/> and drives it at a fixed 10 ticks/s × speed, decoupled from the framerate.
/// The rest of the view only reads the world and sends commands through <see cref="Send"/>.
/// The world is only touched from the main thread, except during <see cref="AdvanceYears"/>, when the
/// view is frozen (<see cref="IsBusy"/>) and nothing reads it.
/// </summary>
public partial class SimHost : Node
{
    public static readonly int[] Speeds = { 0, 1, 2, 4, 8 };
    private const string DataDir = "res://data";
    private const string SavePath = "user://saves/quick.ivsave";
    private const int MaxTicksPerFrame = 120;
    private const double SecondsPerTick = 1.0 / SimTime.TicksPerSecondAt1x;

    [Export] public string ScenarioId { get; set; } = "mvp_start";
    [Export] public ulong Seed { get; set; } = 42;

    private double _accumulator;
    private Task? _advanceTask;
    private long _advanceTarget;
    private long _advanceStart;

    public World World { get; private set; } = null!;
    public ContentDb Content { get; private set; } = null!;
    public ScenarioDef Scenario { get; private set; } = null!;
    public int Speed { get; private set; } = 1;
    public bool Paused => Speed == 0;
    public bool IsBusy => _advanceTask is { IsCompleted: false };

    /// <summary>Fraction (0..1) of the next tick already elapsed — for smooth interpolation.</summary>
    public float TickAlpha => (float)(_accumulator / SecondsPerTick);

    public double AdvanceProgress =>
        _advanceTarget <= _advanceStart ? 1 : Math.Clamp((double)(World.Tick - _advanceStart) / (_advanceTarget - _advanceStart), 0, 1);

    /// <summary>Raised after the world object is replaced (new game, load, long advance): views rebuild.</summary>
    public event Action? WorldReplaced;
    /// <summary>Sim events drained this frame.</summary>
    public event Action<List<SimEvent>>? EventsReceived;
    /// <summary>Messages for the player (save/load results, errors).</summary>
    public event Action<string>? Message;

    public override void _Ready()
    {
        Content = ContentLoader.Load(ReadDataFiles());
        Scenario = ContentLoader.LoadScenario(ReadText($"{DataDir}/{DataPaths.ScenarioDir}/{ScenarioId}.json"), Content,
            ScenarioId + ".json");
        NewGame();
    }

    public void NewGame()
    {
        World = World.Create(Content, Scenario, Seed);
        World.CollectEvents = true;
        _accumulator = 0;
        _objectiveFloor = 0;
        WorldReplaced?.Invoke();
    }

    /// <summary>Read-only data for the HUD (the UI never touches the World directly).</summary>
    private int _objectiveFloor;

    public UiSnapshot BuildUiSnapshot() =>
        UiSnapshotBuilder.Build(World, Speed, key => TranslationServer.Translate(key), ref _objectiveFloor);

    private SessionLog? _log;

    public void Send(SimCommand command)
    {
        if (IsBusy) return;
        (_log ??= new SessionLog()).Command(World, command);
        World.Enqueue(command);
        World.ApplyPendingCommands();   // instant feedback, also while paused (same tick: deterministic)
    }

    public void SetSpeed(int speed)
    {
        Speed = Array.IndexOf(Speeds, speed) >= 0 ? speed : 1;
        if (Speed == 0) _accumulator = 0;
    }

    public override void _Process(double delta)
    {
        if (IsBusy) return;
        if (_advanceTask is not null) FinishAdvance();

        if (!Paused)
        {
            _accumulator += delta * Speed;
            int ticks = 0;
            while (_accumulator >= SecondsPerTick && ticks < MaxTicksPerFrame)
            {
                World.Step();
                _accumulator -= SecondsPerTick;
                ticks++;
            }
            if (ticks == MaxTicksPerFrame) _accumulator = 0;   // can't keep up: drop time instead of spiralling
        }

        _log?.Tick(World);
        var events = World.DrainEvents();
        if (events.Count > 0)
        {
            (_log ??= new SessionLog()).Events(World, events);
            EventsReceived?.Invoke(events);
        }
    }

    /// <summary>Runs N years on a worker thread. The SceneTree is never touched from that thread.</summary>
    public void AdvanceYears(int years)
    {
        if (IsBusy || years <= 0) return;
        var world = World;
        _advanceStart = world.Tick;
        _advanceTarget = world.Tick + (long)years * SimTime.TicksPerYear;
        world.CollectEvents = false;
        _advanceTask = Task.Run(() => world.StepYears(years));
    }

    private void FinishAdvance()
    {
        var task = _advanceTask!;
        _advanceTask = null;
        World.CollectEvents = true;
        if (task.IsFaulted) Message?.Invoke($"Erro ao avançar: {task.Exception?.GetBaseException().Message}");
        else Message?.Invoke($"Avançou até {DateText(World.Calendar)}");
        _accumulator = 0;
        _objectiveFloor = 0;
        WorldReplaced?.Invoke();
    }

    public void QuickSave()
    {
        if (IsBusy) return;
        DirAccess.MakeDirRecursiveAbsolute("user://saves");
        using var file = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write);
        if (file is null)
        {
            Message?.Invoke($"Falha ao salvar: {FileAccess.GetOpenError()}");
            return;
        }
        file.StoreBuffer(SaveSerializer.Save(World));
        Message?.Invoke($"Jogo salvo ({DateText(World.Calendar)})");
    }

    public void QuickLoad()
    {
        if (IsBusy) return;
        if (!FileAccess.FileExists(SavePath))
        {
            Message?.Invoke("Nenhum save rápido encontrado");
            return;
        }
        try
        {
            var result = SaveSerializer.Load(FileAccess.GetFileAsBytes(SavePath), Content);
            World = result.World;
            World.CollectEvents = true;
            _accumulator = 0;
            foreach (var w in result.Warnings) Message?.Invoke(w);
            Message?.Invoke($"Jogo carregado ({DateText(World.Calendar)})");
            _objectiveFloor = 0;
        WorldReplaced?.Invoke();
        }
        catch (Exception e) when (e is SaveException or ContentException)
        {
            Message?.Invoke($"Falha ao carregar: {e.Message}");
        }
    }

    public static string DateText(Calendar cal)
    {
        string season = cal.Season switch
        {
            Season.Spring => "Primavera",
            Season.Summer => "Verão",
            Season.Autumn => "Outono",
            _ => "Inverno",
        };
        return $"Ano {cal.Year} · {season} · mês {cal.MonthOfYear + 1} · dia {cal.DayOfMonth + 1}";
    }

    private static Dictionary<string, string> ReadDataFiles()
    {
        var files = new Dictionary<string, string>();
        foreach (var name in ContentLoader.RequiredFiles) files[name] = ReadText($"{DataDir}/{name}");
        return files;
    }

    private static string ReadText(string path)
    {
        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read)
            ?? throw new ContentException($"{path}: {FileAccess.GetOpenError()}");
        return file.GetAsText();
    }
}
