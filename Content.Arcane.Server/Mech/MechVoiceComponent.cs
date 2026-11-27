namespace Content.Arcane.Server.Mech;

[RegisterComponent, Access(typeof(MechVoiceSystem))]
public sealed partial class MechVoiceComponent : Component
{
    [DataField]
    public string? Effect = "robotic";
}
