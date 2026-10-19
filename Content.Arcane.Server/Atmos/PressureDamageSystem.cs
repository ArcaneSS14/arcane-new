using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;

namespace Content.Arcane.Server.Atmos;

public sealed partial class PressureDamageSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damage = default!;
    [Dependency] private BodySystem _body = default!;

    private const float InitialLowDamage = 2f;
    private const float MaximumLowDamage = 5f;
    private const float RampDuration = 60f;
    private const float HighPressureCoefficient = 2f;
    private const float MaximumHighDamage = 2f;
    private const float ReferenceDamage = 0.6f; // Existing human barotrauma weight (Blunt + Heat).

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BarotraumaComponent, BarotraumaDamageEvent>(OnPressureDamage);
    }

    private void OnPressureDamage(Entity<BarotraumaComponent> ent, ref BarotraumaDamageEvent args)
    {
        float rate;
        if (args.Pressure < Atmospherics.HazardLowPressure)
        {
            var exposure = EnsureComp<PressureExposureComponent>(ent);
            rate = InitialLowDamage + (MaximumLowDamage - InitialLowDamage) * exposure.Elapsed / RampDuration;
            rate *= Math.Clamp(1f - args.Pressure / Atmospherics.HazardLowPressure, 0f, 1f);
            exposure.Elapsed = Math.Min(exposure.Elapsed + args.UpdateInterval, RampDuration);
        }
        else
        {
            RemComp<PressureExposureComponent>(ent);
            if (args.Pressure > Atmospherics.HazardLowPressure && args.Pressure < Atmospherics.HazardHighPressure)
                return;

            rate = args.Pressure >= Atmospherics.HazardHighPressure
                ? Math.Min((args.Pressure / Atmospherics.HazardHighPressure - 1f) * HighPressureCoefficient, MaximumHighDamage)
                : 0f;
        }

        if (args.CurrentDamage >= ent.Comp.MaxDamage)
            return;

        var damage = ent.Comp.Damage * (rate * args.UpdateInterval / ReferenceDamage * _body.GetVitalBodyPartRatio(ent.Owner));
        var remaining = ent.Comp.MaxDamage - args.CurrentDamage;
        var total = damage.GetTotal() * Math.Max(1f, _damage.UniversalAllDamageModifier);
        if (total > remaining)
        {
            foreach (var (type, amount) in damage.DamageDict)
            {
                damage.DamageDict[type] = remaining * amount / total;
            }
        }

        damage.Flags |= DamageSpecifier.DamageFlags.NoWounds | DamageSpecifier.DamageFlags.NoBleeding;
        args.Damage = damage;
    }
}
