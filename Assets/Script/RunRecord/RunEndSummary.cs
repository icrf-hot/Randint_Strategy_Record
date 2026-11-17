using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Randint.Data;

/// <summary>Immutable end-of-run report calculated from the same owned Cache bonuses as combat.</summary>
public sealed class RunEndSummary
{
    public sealed class StatChange
    {
        public string Key { get; }
        public double BaseValue { get; }
        public double FinalValue { get; }
        public double Bonus => FinalValue - BaseValue;
        public double? Percent => BaseValue == 0 ? (double?)null : Bonus / BaseValue * 100;

        public StatChange(string key, double baseValue, double finalValue)
        {
            Key = key;
            BaseValue = baseValue;
            FinalValue = finalValue;
        }
    }

    public sealed class OperatorResult
    {
        public string Name { get; }
        public OperatorPosition Position { get; }
        public IReadOnlyList<StatChange> Stats { get; }

        public OperatorResult(OperatorData data, CacheStatBonuses bonuses)
        {
            Name = data.OperatorName;
            Position = data.Position;
            Stats = Array.AsReadOnly(new[] {
                new StatChange("HP", data.MaxHP, CacheStatBonuses.Add(data.MaxHP, bonuses.MaxHP)),
                new StatChange("ATK", data.Attack, CacheStatBonuses.Add(data.Attack, bonuses.Attack)),
                new StatChange("DEF", data.Defense, CacheStatBonuses.Add(data.Defense, bonuses.Defense)),
                new StatChange("RES", data.ArtsResistance, CacheStatBonuses.Add(data.ArtsResistance, bonuses.ArtsResistance)),
                new StatChange("SPD", data.AttackSpeed, Math.Min(float.MaxValue, (double)data.AttackSpeed + bonuses.AttackSpeed))
            }.OrderByDescending(stat => stat.Percent ?? double.NegativeInfinity).ToArray());
        }
    }

    public bool Cleared { get; }
    public string EndReason { get; }
    public bool HiddenRoute { get; }
    public int Floor { get; }
    public int Integrity { get; }
    public int Consistency { get; }
    public IReadOnlyList<string> Purchased { get; }
    public IReadOnlyList<string> Owned { get; }
    public IReadOnlyList<OperatorResult> Operators { get; }

    private RunEndSummary(RunSaveBindings bindings)
    {
        Cleared = MapRunState.EndReason == "Ending";
        EndReason = MapRunState.EndReason;
        HiddenRoute = MapRunState.HiddenEndingRouteUnlocked;
        Floor = MapRunState.MapFloor;
        Integrity = MapRunState.RecordHealth.Integrity;
        Consistency = MapRunState.RecordHealth.Consistency;
        Purchased = Array.AsReadOnly(MapRunState.Caches.PurchasedIds
            .Select(id => bindings.Cache(id)?.DisplayName ?? id).OrderBy(name => name, StringComparer.Ordinal).ToArray());
        Owned = Array.AsReadOnly(MapRunState.Caches.Owned.Select(item => item.DisplayName)
            .OrderBy(name => name, StringComparer.Ordinal).ToArray());
        var bonuses = new CacheStatBonuses(MapRunState.Caches.Owned);
        Operators = Array.AsReadOnly(MapRunState.PartyIds.Select(bindings.Operator).Where(data => data != null)
            .OrderBy(data => data.Position).Select(data => new OperatorResult(data, bonuses)).ToArray());
    }

    public static RunEndSummary Capture() => new RunEndSummary(RunSaveBindings.Instance);
    public static string Text(string key, params object[] args) => string.Format(GameData.Text("run_summary." + key), args);

    public string Report()
    {
        var report = new StringBuilder();
        report.AppendLine(Text("route", Text(HiddenRoute ? "hidden" : "normal"), Floor));
        report.AppendLine(Text("health", Integrity, Consistency));
        report.AppendLine();
        AppendItems(report, "purchased", Purchased);
        AppendItems(report, "owned", Owned);
        report.AppendLine(Text("stats"));
        report.AppendLine(Text("sort_note"));
        foreach (var op in Operators)
        {
            report.AppendLine();
            report.AppendLine(op.Position.ToString().ToUpperInvariant() + " / " + op.Name);
            foreach (var stat in op.Stats)
            {
                string percent = stat.Percent.HasValue ? stat.Percent.Value.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) + "%" : Text("zero_base");
                report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "{0,-3} {1,8:0.##} → {2,8:0.##}  ({3:+0.##;-0.##;0} / {4})",
                    stat.Key, stat.BaseValue, stat.FinalValue, stat.Bonus, percent));
            }
        }
        return report.ToString();
    }

    private static void AppendItems(StringBuilder report, string key, IReadOnlyList<string> items)
    {
        report.AppendLine(Text(key, items.Count));
        if (items.Count == 0) report.AppendLine(Text("none"));
        foreach (string item in items) report.AppendLine("• " + item);
        report.AppendLine();
    }
}
