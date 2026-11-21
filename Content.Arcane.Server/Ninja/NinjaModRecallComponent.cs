namespace Content.Arcane.Server.Ninja;

[RegisterComponent, Access(typeof(NinjaModRecallSystem))]
public sealed partial class NinjaModRecallComponent : Component;

[RegisterComponent, Access(typeof(NinjaModRecallSystem))]
public sealed partial class NinjaKatanaReturningComponent : Component
{
    public EntityUid Module;
    public EntityUid Wearer;
}
