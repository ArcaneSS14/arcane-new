// Arcane-Start
namespace Content.Goobstation.Shared.Supermatter.Events;

/// <summary>Raised when the delamination countdown completes.</summary>
public sealed class SupermatterDelaminatedEvent(EntityUid supermatter) : EntityEventArgs
{
    public readonly EntityUid Supermatter = supermatter;
}
// Arcane-End
