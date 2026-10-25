// Arcane-Start
using Robust.Shared.Serialization;

namespace Content.Shared.Interaction;

[Serializable, NetSerializable]
public sealed class ArcaneInteractionParticleEvent(
    NetEntity performer,
    NetEntity? used,
    NetEntity target,
    ArcaneInteractionParticleType type) : EntityEventArgs
{
    public NetEntity Performer = performer;
    public NetEntity? Used = used;
    public NetEntity Target = target;
    public ArcaneInteractionParticleType Type = type;
}

[Serializable, NetSerializable]
public enum ArcaneInteractionParticleType
{
    Use,
    Pull,
    InHand,
}
// Arcane-End
