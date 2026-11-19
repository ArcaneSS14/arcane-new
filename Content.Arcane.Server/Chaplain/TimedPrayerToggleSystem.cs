using Content.Goobstation.Shared.Religion.Nullrod;
using Content.Shared.Bible.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Timing.Systems;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Chaplain;

public sealed partial class TimedPrayerToggleSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private ItemToggleSystem _toggle = default!;
    [Dependency] private UseDelaySystem _delay = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TimedPrayerToggleComponent, AlternatePrayEvent>(OnPray);
    }

    private void OnPray(Entity<TimedPrayerToggleComponent> ent, ref AlternatePrayEvent args)
    {
        if (!HasComp<BibleUserComponent>(args.User)
            || !_hands.IsHolding(args.User, ent)
            || ent.Comp.DeactivateAt != null
            || _delay.IsDelayed(ent.Owner)
            || !_toggle.TryActivate(ent.Owner, args.User, predicted: false))
            return;

        _delay.TryResetDelay(ent.Owner);
        ent.Comp.DeactivateAt = _timing.CurTime + ent.Comp.Duration;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<TimedPrayerToggleComponent, ItemToggleComponent>();
        while (query.MoveNext(out var uid, out var prayer, out var toggle))
        {
            if (prayer.DeactivateAt is not { } deactivateAt || _timing.CurTime < deactivateAt)
                continue;

            if (_toggle.TryDeactivate((uid, toggle), predicted: false))
                prayer.DeactivateAt = null;
        }
    }
}
