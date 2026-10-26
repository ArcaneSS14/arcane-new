using Content.Arcane.Shared.Silicons.Synths.Battery;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Body;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Power.EntitySystems;

namespace Content.Arcane.Server.Silicons.Synths.Battery;

public sealed partial class SynthBatteryMetabolismSystem : EntitySystem
{
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private SynthBatterySystem _synthBattery = default!;

    [SubscribeLocalEvent]
    private void OnSatiate(
        Entity<SynthBatteryComponent> ent,
        ref EntityEffectEvent<Satiate> args)
    {
        if (args.Effect.SatiationType != SatiationSystem.Hunger)
            return;

        var charge = args.Effect.Factor * args.Scale * ent.Comp.NutrimentChargeMultiplier;
        if (charge <= 0f || !_synthBattery.TryGetBattery(ent.Owner, out var battery))
            return;

        _battery.ChangeCharge(battery.Value.AsNullable(), charge);
    }
}
