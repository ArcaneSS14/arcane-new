namespace Content.Server.Atmos.Piping.Components
{
    [RegisterComponent]
    public sealed partial class AtmosUnsafeUnanchorComponent : Component
    {
        // Arcane-Start: Repairable machines must survive breakage.
        [DataField]
        public bool DestroyOnBreak = true;
        // Arcane-End

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("enabled")]
        public bool Enabled { get; set; } = true;
    }
}
