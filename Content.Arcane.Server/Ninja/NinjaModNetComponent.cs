namespace Content.Arcane.Server.Ninja;

[RegisterComponent, Access(typeof(NinjaModNetSystem))]
public sealed partial class NinjaModNetComponent : Component;

[RegisterComponent, Access(typeof(NinjaModNetSystem))]
public sealed partial class NinjaNetProjectileComponent : Component
{
    public EntityUid Module;
}

[RegisterComponent, Access(typeof(NinjaModNetSystem))]
public sealed partial class NinjaNetComponent : Component
{
    public EntityUid Target;
    public EntityUid Module;
}
