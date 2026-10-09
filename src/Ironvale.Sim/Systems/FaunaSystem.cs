namespace Ironvale.Sim.Systems;

/// <summary>
/// Wild animals (GDD v0.3 §9), every tick. Deer and rabbits graze and wander around their herd's home and bolt from
/// colonists; wolves roam in autumn and winter, stalk a lone colonist who is away from a burning fire (scaring them
/// drops the load) and back off when three colonists stand together. Herds grow back each spring. Steps are greedy
/// (no distance field per animal); a blocked animal just picks another goal. Uses the "fauna" random stream, which
/// only exists on generated maps.
/// </summary>
public sealed class FaunaSystem : ISimSystem
{
    private const int WanderRadius = 8;
    private const int StalkRadius = 10;
    private const int LoneRadius = 3;
    private const int FireSafeRadius = 6;
    private const int RetreatTicks = 2 * SimTime.TicksPerDay;

    public string Name => "fauna";
    public Phase Phase => Phase.Transport;
    public Frequency Frequency => Frequency.Tick;

    public void Run(World w, in Calendar cal)
    {
        if (w.Animals.Count == 0) return;
        if (cal.IsYearStart && w.Tick != w.StartTick) Breed(w);   // each new spring, not the first tick
        bool coldSeason = cal.Season is Season.Autumn or Season.Winter;
        foreach (var a in w.Animals.ToArray()) Update(w, a, coldSeason);
    }

    private static int WalkTicks(Animal a) => a.Kind switch { FaunaKind.Deer => 6, FaunaKind.Rabbit => 5, _ => 5 };
    private static int RunTicks(Animal a) => a.Kind == FaunaKind.Wolf ? 3 : 2;

    private static void Update(World w, Animal a, bool coldSeason)
    {
        if (a.IsMoving)
        {
            if (--a.StepTicks > 0) return;
            a.Pos = a.Next;
        }
        var bal = w.Content.Balance;
        if (a.Kind == FaunaKind.Wolf)
        {
            Wolf(w, a, coldSeason);
            return;
        }

        int flee = a.Kind == FaunaKind.Deer ? bal.DeerFleeCells : bal.RabbitFleeCells;
        var threat = NearestColonist(w, a.Pos, flee);
        if (threat is not null) Startle(w, a, threat);
        if (a.State == AnimalState.Fleeing)
        {
            var from = w.GetUnit(a.OtherId);
            if (--a.Timer <= 0 || from is null)
            {
                a.State = AnimalState.Grazing;
                a.Timer = 20;
                return;
            }
            StepAway(w, a, from.Pos, RunTicks(a));
            return;
        }
        Wander(w, a);
    }

    /// <summary>Spooked (a colonist came close, or a shot missed): runs away from the unit for a while.</summary>
    internal static void Startle(World w, Animal a, Unit from)
    {
        a.State = AnimalState.Fleeing;
        a.OtherId = from.Id;
        a.Timer = 24;
    }

    internal static void Retreat(World w, Animal wolf)
    {
        wolf.State = AnimalState.Retreating;
        wolf.Timer = RetreatTicks;
        wolf.OtherId = 0;
    }

    private static void Wolf(World w, Animal a, bool coldSeason)
    {
        var bal = w.Content.Balance;
        if (a.State == AnimalState.Retreating)
        {
            if (--a.Timer <= 0) a.State = AnimalState.Grazing;
            else if (a.Pos != a.Home) Step(w, a, w.GreedyStep(a.Pos, a.Home), RunTicks(a));
            return;
        }
        if (!coldSeason)
        {
            a.State = AnimalState.Grazing;
            Wander(w, a);
            return;
        }
        if (w.ColonistsNear(a.Pos, 4, bal.WolfScareGroup))
        {
            Retreat(w, a);
            return;
        }
        var prey = a.State == AnimalState.Stalking ? w.GetUnit(a.OtherId) : null;
        if (prey is null || !Lone(w, prey) || World.Chebyshev(prey.Pos, a.Pos) > StalkRadius)
        {
            prey = w.Units.Where(u => u.IsColonist && u.Controllable && u.Step != UnitStep.Fleeing && Lone(w, u)
                                      && World.Chebyshev(u.Pos, a.Pos) <= bal.WolfThreatCells)
                .OrderBy(u => World.Chebyshev(u.Pos, a.Pos)).ThenBy(u => u.Id).FirstOrDefault();
            if (prey is null)
            {
                a.State = AnimalState.Grazing;
                Wander(w, a);
                return;
            }
            a.State = AnimalState.Stalking;
            a.OtherId = prey.Id;
        }
        if (World.Chebyshev(prey.Pos, a.Pos) <= 1)
        {
            UnitSystem.Scared(w, prey, a);
            Retreat(w, a);
            return;
        }
        Step(w, a, w.GreedyStep(a.Pos, prey.Pos), RunTicks(a));
    }

    /// <summary>No other colonist within reach and no burning fire nearby: easy prey for a wolf's bluff.</summary>
    private static bool Lone(World w, Unit u) =>
        !w.ColonistsNear(u.Pos, LoneRadius, 2) && !w.FireNear(u.Pos, FireSafeRadius);

    /// <summary>Nearest colonist within <paramref name="radius"/>; hunters stalking quietly don't spook game (a miss does).</summary>
    private static Unit? NearestColonist(World w, Cell c, int radius) =>
        w.Units.Where(u => u.IsColonist && u.Order is not { Kind: OrderKind.Hunt } && World.Chebyshev(u.Pos, c) <= radius)
            .OrderBy(u => World.Chebyshev(u.Pos, c)).ThenBy(u => u.Id).FirstOrDefault();

    private static void Wander(World w, Animal a)
    {
        if (a.State == AnimalState.Grazing)
        {
            if (--a.Timer > 0) return;
            var rng = w.Rng.Get(RngStreams.Fauna);
            var goal = new Cell(a.Home.X + rng.Range(-WanderRadius, WanderRadius), a.Home.Y + rng.Range(-WanderRadius, WanderRadius));
            if (!w.Map.InBounds(goal) || w.Terrain!.IsWater(goal))
            {
                a.Timer = 10;
                return;
            }
            a.Goal = goal;
            a.State = AnimalState.Walking;
        }
        var next = w.GreedyStep(a.Pos, a.Goal);
        if (next == a.Pos)
        {
            a.State = AnimalState.Grazing;
            a.Timer = w.Rng.Get(RngStreams.Fauna).Range(20, 60);
            return;
        }
        Step(w, a, next, WalkTicks(a));
    }

    private static void StepAway(World w, Animal a, Cell from, int ticks)
    {
        Cell best = a.Pos;
        int bestD = a.Pos.Manhattan(from);
        foreach (var (dx, dy) in Terrain.Dirs)
        {
            var n = new Cell(a.Pos.X + dx, a.Pos.Y + dy);
            if (!w.Map.InBounds(n) || !w.Map.CanStep(a.Pos, n)) continue;
            int d = n.Manhattan(from);
            if (d > bestD)
            {
                bestD = d;
                best = n;
            }
        }
        if (best != a.Pos) Step(w, a, best, ticks);
    }

    private static void Step(World w, Animal a, Cell next, int ticks)
    {
        if (next == a.Pos) return;
        a.Next = next;
        a.StepTicks = ticks;
    }

    /// <summary>Each spring: deer herds with a pair grow by one (up to 8), rabbits by two (up to 6).</summary>
    private static void Breed(World w)
    {
        foreach (var herd in w.Animals.GroupBy(a => a.Herd).OrderBy(g => g.Key).ToList())
        {
            var first = herd.OrderBy(a => a.Id).First();
            int count = herd.Count();
            (int add, int max) = first.Kind switch { FaunaKind.Deer => (1, 8), FaunaKind.Rabbit => (2, 6), _ => (0, 0) };
            for (int i = 0; i < add && count >= 2 && count < max; i++, count++)
                w.AddAnimal(first.Kind, first.Herd, first.Home, first.Home);
        }
    }
}
