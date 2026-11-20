namespace Content.Arcane.Shared.Weapons.Aim;

[RegisterComponent]
public sealed partial class ArcaneAimPvsComponent : Component
{
    [DataField]
    public float Increase = 0.8f;
}
