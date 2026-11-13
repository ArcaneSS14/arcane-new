// Arcane-Start
namespace Content.Medical.Shared.Surgery;

/// <summary>Raised on the patient after successful organ insertion, before surgeon-side step bookkeeping.</summary>
[ByRefEvent]
public readonly record struct SurgeryOrganAddedEvent(EntityUid User, EntityUid Organ);
// Arcane-End
