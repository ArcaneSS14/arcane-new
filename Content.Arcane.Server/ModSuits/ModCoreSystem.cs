using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Content.Shared.Stacks;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;

namespace Content.Arcane.Server.ModSuits;

public sealed partial class ModCoreSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStackSystem _stack = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ModCoreRefuelableComponent, InteractUsingEvent>(OnRefuel);
    }

    private void OnRefuel(Entity<ModCoreRefuelableComponent> core, ref InteractUsingEvent args)
    {
        if (args.Handled
            || !_whitelist.IsValid(core.Comp.Fuel, args.Used)
            || !float.IsFinite(core.Comp.ChargePerFuel)
            || core.Comp.ChargePerFuel <= 0f
            || !_powerCell.TryGetBatteryFromEntityOrSlot(core.Owner, out var battery))
            return;

        args.Handled = true;
        var source = battery.Value.AsNullable();
        var missing = battery.Value.Comp.MaxCharge - _battery.GetCharge(source);
        if (missing <= 0f)
        {
            _popup.PopupEntity(Loc.GetString("modsuit-core-full"), core, args.User);
            return;
        }

        var consumed = 1;
        if (TryComp(args.Used, out StackComponent? stack))
        {
            var available = _stack.GetCount((args.Used, stack));
            if (available <= 0)
                return;

            consumed = (int) MathF.Min(available, MathF.Ceiling(missing / core.Comp.ChargePerFuel));
            if (!_stack.TryUse((args.Used, stack), consumed))
                return;
        }
        else
        {
            Del(args.Used);
        }

        _battery.ChangeCharge(source, consumed * core.Comp.ChargePerFuel);
        _audio.PlayPvs(core.Comp.RefuelSound, core);
        _popup.PopupEntity(Loc.GetString("modsuit-core-refueled"), core, args.User);
    }
}
