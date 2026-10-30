namespace Content.Server.Research.Components;

[RegisterComponent]
public sealed partial class ResearchPointSourceComponent : Component
{
    // Arcane-Start
    [DataField]
    public string PointType = "General";
    // Arcane-End

    [DataField("pointspersecond"), ViewVariables(VVAccess.ReadWrite)]
    public int PointsPerSecond;

    // Arcane-Start
    [DataField]
    public string? RequiredInfrastructure;
    // Arcane-End

    [DataField("active"), ViewVariables(VVAccess.ReadWrite)]
    public bool Active;
}
