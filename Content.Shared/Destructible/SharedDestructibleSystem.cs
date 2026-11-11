using Content.Shared.Damage.Systems;

namespace Content.Shared.Destructible;

public abstract partial class SharedDestructibleSystem : EntitySystem
{
    // TODO: I don't really like this but this is out of scope to re-do destructible triggers while refactoring damageable
    [Dependency] public DamageableSystem Damageable = default!;

    /// <summary>
    /// Force entity to be destroyed and deleted.
    /// </summary>
    public bool DestroyEntity(EntityUid owner) => DestroyEntity(owner, null); // Arcane-Edit: inline implementation > compatible overload

    // Arcane-Start
    public bool DestroyEntity(EntityUid owner, EntityUid? cause)
    // Arcane-End
    {
        if (!CanDestroy(owner))
            return false;

        var eventArgs = new DestructionEventArgs { Cause = cause }; // Arcane-Edit: no cause > pass cause
        RaiseLocalEvent(owner, eventArgs);

        PredictedQueueDel(owner);
        return true;
    }

    /// <param name="owner">Entity that your checking.</param>
    /// <returns>If it can be destroyed</returns>
    public bool CanDestroy(EntityUid owner)
    {
        var ev = new DestructionAttemptEvent();
        RaiseLocalEvent(owner, ev);
        if (ev.Cancelled)
            return false;

        return true;
    }

    /// <summary>
    /// Force entity to break.
    /// </summary>
    public void BreakEntity(EntityUid owner)
    {
        var eventArgs = new BreakageEventArgs();
        RaiseLocalEvent(owner, eventArgs);
    }
}

/// <summary>
/// Raised before an entity is about to be destroyed and deleted
/// </summary>
public sealed class DestructionAttemptEvent : CancellableEntityEventArgs
{

}

/// <summary>
/// Raised when entity is destroyed and about to be deleted.
/// </summary>
public sealed class DestructionEventArgs : EntityEventArgs
{
    public EntityUid? Cause; // Arcane
}

/// <summary>
/// Raised when entity was heavy damage and about to break.
/// </summary>
public sealed class BreakageEventArgs : EntityEventArgs
{

}
