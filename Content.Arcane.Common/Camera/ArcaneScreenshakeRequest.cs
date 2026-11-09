using Robust.Shared.Player;

namespace Content.Arcane.Common.Camera;

/// <summary>
/// Requests a camera impulse without depending on its gameplay implementation.
/// </summary>
[ByRefEvent]
public readonly record struct ArcaneScreenshakeRequest(EntityUid Target, ArcaneScreenshakeParameters? Translation, ArcaneScreenshakeParameters? Rotation)
{
    public static void Raise(IEntityManager entities, EntityUid target, ArcaneScreenshakeParameters? translation, ArcaneScreenshakeParameters? rotation)
    {
        var request = new ArcaneScreenshakeRequest(target, translation, rotation);
        entities.EventBus.RaiseEvent(EventSource.Local, ref request);
    }

    public static void Raise(IEntityManager entities, Filter filter, ArcaneScreenshakeParameters? translation, ArcaneScreenshakeParameters? rotation)
    {
        foreach (var session in filter.Recipients)
        {
            if (session.AttachedEntity is { } target)
                Raise(entities, target, translation, rotation);
        }
    }
}
