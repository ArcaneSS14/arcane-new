namespace Content.Arcane.Common.Interaction;

/// <summary>Requests an immediate refresh of facing constraints after an action changes.</summary>
[ByRefEvent]
public readonly record struct ForcedFacingRefreshRequest(EntityUid Target)
{
    public static void Raise(IEntityManager entities, EntityUid target)
    {
        var request = new ForcedFacingRefreshRequest(target);
        entities.EventBus.RaiseEvent(EventSource.Local, ref request);
    }
}
