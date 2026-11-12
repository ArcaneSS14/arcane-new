using Content.Shared.IdentityManagement;

namespace Content.Arcane.Shared.Gambling;

public sealed partial class EvilClawPrizeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EvilClawPrizeComponent, GetEntityDisplayNameEvent>(OnDisplayName);
    }

    private void OnDisplayName(Entity<EvilClawPrizeComponent> ent, ref GetEntityDisplayNameEvent args)
        => args.Name = Loc.GetString("arcane-clawmachine-evil-prize-name", ("name", args.Name));
}
