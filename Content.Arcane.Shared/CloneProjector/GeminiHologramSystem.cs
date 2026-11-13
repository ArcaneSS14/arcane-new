using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Arcane.Shared.CloneProjector;

public sealed partial class GeminiHologramSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GeminiHologramComponent, ShotAttemptedEvent>(OnShotAttempt);
    }

    private void OnShotAttempt(Entity<GeminiHologramComponent> ent, ref ShotAttemptedEvent args)
    {
        if (!ent.Comp.RestrictRangedWeapons)
            return;

        args.Cancel();
        _popup.PopupClient(Loc.GetString("gun-disabled"), ent, ent);
    }
}
