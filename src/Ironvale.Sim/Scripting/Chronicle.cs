namespace Ironvale.Sim.Scripting;

/// <summary>
/// "Enquanto você estava fora" (Marco 2B): watches the world while time is fast-forwarded and keeps the memorable
/// moments — families leaving, hungry or cold winters, winters without losses, the reeve's busiest years, overload,
/// bumper harvests, what got built. Observer only: it reads events and state, never changes the world.
/// Moments carry a translation key ("chronicle.&lt;key&gt;") and arguments for the UI.
/// </summary>
public sealed class Chronicle
{
    public sealed record Moment(int Year, Season Season, string Key, string[] Args, int Weight);

    private readonly List<Moment> _moments = new();
    private int _year = -1;
    private Season _season;
    private readonly Dictionary<string, int> _built = new(StringComparer.Ordinal);
    private readonly List<(string Name, string Reason)> _leftThisSeason = new();
    private int _reeveActions;
    private int _overloadDays;
    private long _maxFood;
    private bool _hungryWinter, _coldWinter, _winterLosses;
    private bool _winterSeen;

    public int StartYear { get; private set; } = -1;
    public int StartPopulation { get; private set; }
    public int EndPopulation { get; private set; }
    public int EndDecrees { get; private set; }
    private int _days;

    /// <summary>Whole years observed (days / 360).</summary>
    public int Years => _days / SimTime.DaysPerYear;
    public IReadOnlyList<Moment> Moments => _moments;

    /// <summary>Call once per simulated day with the events drained that day.</summary>
    public void Observe(World w, IEnumerable<SimEvent> events)
    {
        var cal = w.Calendar;
        _days++;
        if (StartYear < 0)
        {
            StartYear = cal.Year;
            StartPopulation = w.Households.Count;
            _year = cal.Year;
            _season = cal.Season;
        }
        if (cal.Season != _season) CloseSeason(w);
        if (cal.Year != _year) CloseYear(w);

        foreach (var e in events)
        {
            switch (e)
            {
                case HouseholdLeft l:
                    _leftThisSeason.Add((l.Name, l.Reason));
                    if (cal.IsWinter) _winterLosses = true;
                    break;
                case BuildingCompleted b when w.GetBuilding(b.BuildingId) is { } done:
                    _built[done.Def.Id] = _built.GetValueOrDefault(done.Def.Id) + 1;
                    break;
                case PolicyActed:
                    _reeveActions++;
                    break;
                case SuggestionOffered:
                    Add(cal, "suggestion", 2);
                    break;
                case FamilyFormed f:
                    Add(cal, "family_formed", 3, f.Name);
                    break;
                case FamilyArrived a:
                    Add(cal, "family_arrived", 2, a.Name);
                    break;
            }
        }
        if (w.AdminOverload > 0) _overloadDays++;
        _maxFood = Math.Max(_maxFood, w.StorageStock(w.Content.Resource("food").Index).WholeUnits);
        if (cal.IsWinter)
        {
            _winterSeen = true;
            if (w.Households.Any(h => h.FoodDeficitDays > 0)) _hungryWinter = true;
            if (w.Households.Any(h => h.ColdDeficitDays > 0)) _coldWinter = true;
        }
        EndPopulation = w.Households.Count;
        EndDecrees = w.Policies.Count(p => p.Enabled);
    }

    /// <summary>Call when the fast-forward ends (flushes the last season/year).</summary>
    public void Finish(World w)
    {
        if (_year < 0) return;
        CloseSeason(w);
        CloseYear(w);
    }

    private void Add(in Calendar cal, string key, int weight, params string[] args) =>
        _moments.Add(new Moment(cal.Year, cal.Season, key, args, weight));

    private void Add(int year, Season season, string key, int weight, params string[] args) =>
        _moments.Add(new Moment(year, season, key, args, weight));

    private void CloseSeason(World w)
    {
        if (_leftThisSeason.Count > 0)
        {
            string reasons = string.Join(", ", _leftThisSeason.Select(x => x.Reason).Distinct());
            string names = string.Join(", ", _leftThisSeason.Select(x => x.Name));
            Add(_year, _season, "left", 5, _leftThisSeason.Count.ToString(), names, reasons);
            _leftThisSeason.Clear();
        }
        if (_season == Season.Winter && _winterSeen)
        {
            if (_hungryWinter) Add(_year, _season, "hungry_winter", 4);
            if (_coldWinter) Add(_year, _season, "cold_winter", 4);
            if (!_hungryWinter && !_coldWinter && !_winterLosses) Add(_year, _season, "good_winter", 3);
            _hungryWinter = _coldWinter = _winterLosses = _winterSeen = false;
        }
        _season = w.Calendar.Season;
    }

    private void CloseYear(World w)
    {
        if (_built.Count > 0)
        {
            string list = string.Join(", ", _built.OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => $"{kv.Value}× {w.Content.Building(kv.Key).Name}"));
            Add(_year, Season.Winter, "built", 1, list);
            _built.Clear();
        }
        if (_reeveActions >= 10) Add(_year, Season.Winter, "reeve_busy", 2, _reeveActions.ToString());
        if (_overloadDays > 0) Add(_year, Season.Winter, "overloaded", 3, _overloadDays.ToString());
        if (_maxFood >= 2000) Add(_year, Season.Summer, "harvest", 2, _maxFood.ToString());
        _reeveActions = 0;
        _overloadDays = 0;
        _maxFood = 0;
        _year = w.Calendar.Year;
    }

    /// <summary>The most memorable moments (by weight), in chronological order (year, then season).</summary>
    public IReadOnlyList<Moment> Highlights(int max = 12) =>
        _moments.Select((m, i) => (m, i)).OrderByDescending(x => x.m.Weight).ThenBy(x => x.i).Take(max)
            .OrderBy(x => x.m.Year).ThenBy(x => (int)x.m.Season).ThenBy(x => x.i).Select(x => x.m).ToList();
}
