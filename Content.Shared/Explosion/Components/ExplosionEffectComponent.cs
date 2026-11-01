// Arcane-Start
using Content.Shared.Explosion.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Explosion.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedExplosionEffectSystem))]
public sealed partial class ExplosionEffectComponent : Component
{
    /// <summary>
    /// Effect prototypes spawned at the explosive entity's map position.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId> VisualEffects = new()
    {
        "ExplosionEffectGrenade",
        "ExplosionEffectGrenadeShockWave"
    };

    /// <summary>
    /// Optional thrown effect prototypes spawned around the explosion.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<EntProtoId> ShrapnelEffects = new()
    {
        "ExplosionEffectShrapnel1",
        "ExplosionEffectShrapnel2"
    };

    [DataField, AutoNetworkedField]
    public int MinShrapnel = 5;

    [DataField, AutoNetworkedField]
    public int MaxShrapnel = 9;

    [DataField, AutoNetworkedField]
    public float ShrapnelSpeed = 5f;
}
// Arcane-End
