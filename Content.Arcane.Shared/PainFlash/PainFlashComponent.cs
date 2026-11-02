using Content.Shared.FixedPoint;
using Robust.Shared.Timing;

namespace Content.Arcane.Shared.PainFlash;

[RegisterComponent, NetworkedComponent]
public sealed partial class PainFlashComponent : Component;

[Serializable, NetSerializable]
public sealed class PainFlashMessage(FixedPoint2 damage, GameTick tick) : EntityEventArgs
{
    public FixedPoint2 Damage = damage;
    public GameTick Tick = tick;
}
