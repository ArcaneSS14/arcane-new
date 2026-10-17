using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Arcane.Shared.Medical.Concussion;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true, fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class ConcussionThresholdComponent : Component
{
    [DataField, AutoNetworkedField]
    public ConcussionState CurrentState = ConcussionState.Sane;

    [DataField, AutoNetworkedField]
    public FixedPoint2 StoredDamage;

    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, ConcussionState> Thresholds = new();

    [DataField]
    public HashSet<ConcussionState> AllowedStates = new()
    {
        ConcussionState.Sane, ConcussionState.Minor, ConcussionState.Hard, ConcussionState.Overwhelmed,
    };

    [DataField]
    public Dictionary<ConcussionState, float> SpeedModifierThresholds = new();

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public float HealRate = 1f;

    [DataField]
    public FixedPoint2 FallbackDamageCap = 200;

    [DataField]
    public float ExplosionDamageMultiplier = 10f;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(0.5);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ConcussedComponent : Component;

[RegisterComponent]
public sealed partial class ConcussionResistanceComponent : Component
{
    [DataField]
    public float Resistance;
}
