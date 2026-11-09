using Robust.Shared.Input;

namespace Content.Arcane.Common.Input;

[KeyFunctions]
public static class ArcaneKeyFunctions
{
    public static readonly BoundKeyFunction TogglePosing = "TogglePosing";
    public static readonly BoundKeyFunction PosingOffsetLeft = "PosingOffsetLeft";
    public static readonly BoundKeyFunction PosingOffsetRight = "PosingOffsetRight";
    public static readonly BoundKeyFunction PosingOffsetUp = "PosingOffsetUp";
    public static readonly BoundKeyFunction PosingOffsetDown = "PosingOffsetDown";
    public static readonly BoundKeyFunction PosingRotateNegative = "PosingRotateNegative";
    public static readonly BoundKeyFunction PosingRotatePositive = "PosingRotatePositive";
}
