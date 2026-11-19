using Content.Shared.Weapons.Melee.Components;

namespace Content.Arcane.Shared.Tennis;

[RegisterComponent, NetworkedComponent]
public sealed partial class TennisRacketComponent : Component;

public sealed class TennisRacketSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TennisRacketComponent, AttemptMeleeThrowOnHitEvent>(OnAttempt);
    }

    private void OnAttempt(Entity<TennisRacketComponent> ent, ref AttemptMeleeThrowOnHitEvent args)
    {
        if (Prototype(args.Target)?.ID != "TennisBall")
            args.Cancelled = true;
    }
}
