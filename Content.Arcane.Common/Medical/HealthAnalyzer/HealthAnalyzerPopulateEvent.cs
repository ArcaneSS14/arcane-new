namespace Content.Arcane.Common.Medical.HealthAnalyzer;

/// <summary>
/// Collects patient diagnostics without exposing the analyzer's UI controls.
/// </summary>
[ByRefEvent]
public struct HealthAnalyzerPopulateEvent()
{
    public string? Pulse;
    public readonly List<string> Conditions = new();
}
