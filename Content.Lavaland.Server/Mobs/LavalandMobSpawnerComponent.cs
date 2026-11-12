namespace Content.Lavaland.Server.Mobs;

[RegisterComponent]
public sealed partial class LavalandMobSpawnerComponent : Component
{
    [DataField]
    public float ActivationRange = 20f;
}
