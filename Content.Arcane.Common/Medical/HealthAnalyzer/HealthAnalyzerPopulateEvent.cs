using Content.Shared.Damage;

namespace Content.Arcane.Common.Medical.HealthAnalyzer;

/// <summary>
/// Collects patient diagnostics without exposing the analyzer's UI controls.
/// </summary>
[ByRefEvent]
public struct HealthAnalyzerPopulateEvent(EntityUid? part = null, bool includeWounds = true)
{
    public HealthAnalyzerPopulateEvent() : this(null) { }

    public readonly EntityUid? Part = part;
    public readonly bool IncludeWounds = includeWounds;
    /// <summary>
    /// Damage for display, taking the larger of raw damage and wounds per part and damage type.
    /// </summary>
    public DamageSpecifier? DiagnosticDamage;
    public string? Pulse;
    public readonly List<string> Conditions = new();
}
