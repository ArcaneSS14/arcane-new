using Content.Shared.Decals;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Explosion;

/// <summary>
/// Scatters random decals around the entity when triggered.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DecalGrenadeComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// The decals to pick from.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<DecalPrototype>> DecalPrototypes = new();

    /// <summary>
    /// How many decals to spawn.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DecalCount = 25;

    /// <summary>
    /// Radius around the entity in which decals are spawned.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DecalRadius = 3f;
}
