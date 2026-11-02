using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Species.Hydrakin;

[RegisterComponent]
public sealed partial class HydrakinComponent : Component
{
    [DataField]
    public float CoolOffCoefficient = 0.1f;

    [DataField]
    public EntProtoId CoolOffActionId = "ActionHydrakinCoolOff";

    [DataField]
    public SoundSpecifier? CoolOffSound = new SoundCollectionSpecifier("HydrakinFlap");

    [DataField]
    public EntityUid? CoolOffAction;

    [DataField]
    public float PassiveHeatWatts = 3900f;

    [DataField]
    public float CriticalHeatMultiplier = 0.75f;

    [DataField]
    public float MinimumTemperature = 27.15f;

    [DataField]
    public float MaximumTemperature = 340f;
}
