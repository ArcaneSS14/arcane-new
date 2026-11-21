using Robust.Shared.Map;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ModPathfinderComponent : Component
{
    [DataField]
    public EntityUid? Implant;

    public EntityUid? Flight;

    [DataField]
    public string SuitSlot = "back";

    [DataField]
    public TimeSpan Warmup = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan FlightTime = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(5);

    [DataField]
    public float ChargeCost = 8f;

    [DataField, AutoPausedField]
    public TimeSpan NextRecall;
}

[RegisterComponent]
public sealed partial class ModPathfinderImplantComponent : Component
{
    [DataField]
    public EntityUid? Module;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class ModPathfinderFlightComponent : Component
{
    public EntityUid Module;
    public EntityUid Target;

    [AutoPausedField]
    public TimeSpan LaunchAt;

    [AutoPausedField]
    public TimeSpan ArriveAt;

    public MapCoordinates Origin;
    public bool Launched;
    public bool Arrived;
    public bool RestoreCollision;
}
