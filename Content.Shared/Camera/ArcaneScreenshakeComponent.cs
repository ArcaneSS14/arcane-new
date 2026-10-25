// Arcane-Start
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Camera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ArcaneScreenshakeComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ArcaneScreenshakeCommand> Commands = new();

    public override bool SendOnlyToOwner => true;
}

[DataRecord, Serializable, NetSerializable]
public partial record ArcaneScreenshakeCommand(
    ArcaneScreenshakeParameters? Translation,
    ArcaneScreenshakeParameters? Rotation,
    TimeSpan Start,
    TimeSpan End);

[DataDefinition, Serializable, NetSerializable]
public partial record ArcaneScreenshakeParameters()
{
    [DataField(required: true)]
    public float Trauma = 0f;

    [DataField]
    public float DecayRate = 1.2f;

    [DataField]
    public float Frequency = 0.01f;

    public ArcaneScreenshakeParameters(float trauma, float decayRate = 1.2f, float frequency = 0.01f) : this()
    {
        Trauma = trauma;
        DecayRate = decayRate;
        Frequency = frequency;
    }
}
// Arcane-End
