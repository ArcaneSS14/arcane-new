namespace Content.Server.Research.TechnologyDisk.Components;

[RegisterComponent]
public sealed partial class DiskConsolePrintingComponent : Component
{
    public TimeSpan FinishTime;

    // Arcane-Start
    public EntityUid? Actor;

    public EntityUid? Server;

    public int Price;
    // Arcane-End
}
