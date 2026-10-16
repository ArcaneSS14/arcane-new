using System.Linq;
using Content.Arcane.Shared.Medical.Heart;
using Content.Arcane.Shared.Medical.Defibrillator;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Server.GameObjects;

namespace Content.Arcane.Server.Medical.Defibrillator;

public sealed partial class ManualDefibrillatorSystem : EntitySystem
{
    [Dependency] private ItemToggleSystem _itemToggle = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ManualDefibrillatorComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ManualDefibrillatorComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<ManualDefibrillatorComponent, DefibrillatorChargeSettingMessage>(OnSettingChanged);
    }

    private void OnMapInit(Entity<ManualDefibrillatorComponent> ent, ref MapInitEvent args)
    {
        UpdateFibCharge(ent);
    }

    private void OnUiOpened(Entity<ManualDefibrillatorComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey.Equals(ManualDefibrillatorUiKey.Key))
            UpdateUi(ent);
    }

    private void OnSettingChanged(Entity<ManualDefibrillatorComponent> ent, ref DefibrillatorChargeSettingMessage args)
    {
        ent.Comp.SettingsRevision++;
        if (args.ChargeSetting is not { Flips.Length: DefibrillatorChargeSetting.FlipAmount })
        {
            UpdateUi(ent);
            return;
        }

        var powered = args.ChargeSetting.Power && args.ChargeSetting.Flips.Any(flip => flip);
        if (TryComp<ItemToggleComponent>(ent, out var itemToggle)
            && !_itemToggle.TrySetActive((ent.Owner, itemToggle), powered, args.Actor, predicted: false))
        {
            UpdateUi(ent);
            return;
        }

        ent.Comp.ChargeSetting = args.ChargeSetting.Clone();
        UpdateFibCharge(ent);
        UpdateUi(ent);
    }

    private void UpdateFibCharge(Entity<ManualDefibrillatorComponent> ent)
    {
        if (!TryComp<HeartDefibrillatorComponent>(ent, out var defibrillator))
            return;

        var flips = ent.Comp.ChargeSetting.Flips.Count(flip => flip);
        defibrillator.BpmZapHeal = GetCharge(ent.Comp.BpmZapFlip, flips);
        defibrillator.BpmZapHealFlatline = GetCharge(ent.Comp.BpmZapFlatlineFlip, flips);
    }

    private static float GetCharge(Dictionary<int, float> thresholds, int flips)
    {
        var selected = int.MinValue;
        var charge = 0f;
        foreach (var (threshold, value) in thresholds)
        {
            if (threshold > flips || threshold <= selected)
                continue;

            selected = threshold;
            charge = value;
        }
        return charge;
    }

    public void UpdateUi(Entity<ManualDefibrillatorComponent> ent)
    {
        _ui.SetUiState(
            ent.Owner,
            ManualDefibrillatorUiKey.Key,
            new DefibrillatorBuiState(
                ent.Comp.ChargeSetting.Clone(),
                ent.Comp.PulseState,
                ent.Comp.DisplayedBpm,
                ent.Comp.SettingsRevision));
    }
}
