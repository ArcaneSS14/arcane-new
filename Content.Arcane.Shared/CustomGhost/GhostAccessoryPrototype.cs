using Content.Trauma.Shared.Particles;
using Robust.Shared.Utility;

namespace Content.Arcane.Shared.CustomGhost;

public enum GhostAccessoryCategory : byte
{
    Hat,
    Mask,
    Particles,
}

[Prototype]
public sealed partial class GhostAccessoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name { get; private set; }

    [DataField(required: true)]
    public GhostAccessoryCategory Category { get; private set; }

    [DataField]
    public SpriteSpecifier? Sprite { get; private set; }

    [DataField]
    public ProtoId<ParticleEffectPrototype>? ParticleEffect { get; private set; }
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class GhostAccessoriesComponent : Component
{
    [AutoNetworkedField]
    public ProtoId<GhostAccessoryPrototype>? Hat;

    [AutoNetworkedField]
    public ProtoId<GhostAccessoryPrototype>? Mask;

    [AutoNetworkedField]
    public ProtoId<GhostAccessoryPrototype>? Particles;
}

[Serializable, NetSerializable]
public sealed class SetGhostAccessoriesEvent(string hat, string mask, string particles) : EntityEventArgs
{
    public string Hat { get; } = hat;
    public string Mask { get; } = mask;
    public string Particles { get; } = particles;
}
