using Content.Shared.Chemistry.Components;
using Content.Shared.DoAfter;

namespace Content.Arcane.Shared.WallStains;

[Serializable, NetSerializable]
public sealed partial class CleanWallStainDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class PourOnWallDoAfterEvent : SimpleDoAfterEvent;

public sealed class CleanWallStainsEvent(bool transformToWater = false) : EntityEventArgs
{
    public bool TransformToWater = transformToWater;
}

[ByRefEvent]
public readonly record struct WallStainAppliedEvent(EntityUid Wall, Solution Solution);
