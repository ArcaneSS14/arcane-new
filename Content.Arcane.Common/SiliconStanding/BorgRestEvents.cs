// Arcane-Start: hooks between borg selection and its optional resting behavior
namespace Content.Arcane.Common.SiliconStanding;

[ByRefEvent]
public readonly record struct BorgSubtypeSelectedEvent;

[ByRefEvent]
public readonly record struct BorgVisualsUpdatedEvent;

[ByRefEvent]
public readonly record struct BorgRestVisualsChangedEvent(bool Resting);
// Arcane-End
