using Content.Arcane.Shared.ModSuits;
using Content.Arcane.Shared.ModSuits.Components;
using Content.Arcane.Shared.ModSuits.Systems;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;

namespace Content.Arcane.Server.ModSuits;

public sealed partial class ModSuitPressureSystem : EntitySystem
{
    [Dependency] private BarotraumaSystem _barotrauma = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private ModSuitSystem _suits = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ModArmorBoosterModuleComponent, ModModuleActivatedEvent>(OnBoosterActivated);
        SubscribeLocalEvent<ModArmorBoosterModuleComponent, ModModuleDeactivatedEvent>(OnBoosterDeactivated);
        SubscribeLocalEvent<ModPartComponent, GetPressureProtectionValuesEvent>(OnPartPressureProtection);
        SubscribeLocalEvent<ModSphereTransformModuleComponent, ModSpherePressureQuery>(OnSpherePressure);
    }

    private void OnSpherePressure(Entity<ModSphereTransformModuleComponent> module, ref ModSpherePressureQuery args)
    {
        if (module.Comp.FormOwner is { } wearer)
            args.Pressure = _atmosphere.GetTileMixture(wearer)?.Pressure ?? 0f;
    }

    private void OnBoosterActivated(Entity<ModArmorBoosterModuleComponent> module, ref ModModuleActivatedEvent args)
    {
        if (args.Wearer is { } wearer)
            _barotrauma.RefreshPressureProtection(wearer);
    }

    private void OnBoosterDeactivated(Entity<ModArmorBoosterModuleComponent> module, ref ModModuleDeactivatedEvent args)
    {
        if (args.Wearer is { } wearer && !TerminatingOrDeleted(wearer))
            _barotrauma.RefreshPressureProtection(wearer);
    }

    private void OnPartPressureProtection(Entity<ModPartComponent> part, ref GetPressureProtectionValuesEvent args)
    {
        if (_suits.IsPartPressureProtectionSuppressed(part))
            args = new GetPressureProtectionValuesEvent { HighPressureMultiplier = 1f, LowPressureMultiplier = 1f };
    }
}
