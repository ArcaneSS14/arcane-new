// Arcane-Start
using Content.Shared.Atmos;

namespace Content.Goobstation.Shared.Supermatter;

[ByRefEvent]
public readonly record struct SupermatterCycleStartingEvent;

[ByRefEvent]
public readonly record struct SupermatterGasSampleEvent(GasMixture Sample, float TotalMoles, float WasteMultiplier);

[ByRefEvent]
public readonly record struct SupermatterCycleCompletedEvent(bool HasGasSample);
// Arcane-End
