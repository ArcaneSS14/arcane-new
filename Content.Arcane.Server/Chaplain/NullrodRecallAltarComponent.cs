namespace Content.Arcane.Server.Chaplain;

/// <summary>Allows trained chaplains to recall their bound set at an altar.</summary>
[RegisterComponent]
public sealed partial class NullrodRecallAltarComponent : Component
{
    /// <summary>The uninterrupted prayer required before recall.</summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);
}
