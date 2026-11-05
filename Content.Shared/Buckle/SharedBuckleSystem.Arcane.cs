using System.Numerics;
using Content.Shared.Buckle.Components;

namespace Content.Shared.Buckle;

/// <summary>
/// Arcane: per-entity buckle offsets for straps that hold several entities.
/// </summary>
public abstract partial class SharedBuckleSystem
{
    /// <summary>
    /// The offset a buckled entity sits at on this strap.
    /// </summary>
    public static Vector2 GetBuckleOffset(StrapComponent strap, EntityUid buckle)
    {
        return strap.BuckleOffsets.GetValueOrDefault(buckle, strap.BuckleOffset);
    }

    /// <summary>
    /// Sets the offset an entity will be buckled at.
    /// </summary>
    public void SetBuckleOffset(Entity<StrapComponent> strap, EntityUid buckle, Vector2 offset)
    {
        strap.Comp.BuckleOffsets[buckle] = offset;
        Dirty(strap);
    }

    /// <summary>
    /// Sets the default offset for entities without their own offset.
    /// </summary>
    public void SetDefaultBuckleOffset(Entity<StrapComponent> strap, Vector2 offset)
    {
        strap.Comp.BuckleOffset = offset;
        Dirty(strap);
    }
}
