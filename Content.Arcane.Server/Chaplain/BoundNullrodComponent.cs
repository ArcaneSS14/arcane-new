namespace Content.Arcane.Server.Chaplain;

/// <summary>Identifies the mind owning this particular form; the mind owns the complete set.</summary>
[RegisterComponent]
public sealed partial class BoundNullrodComponent : Component
{
    [ViewVariables]
    public EntityUid Mind;
}
