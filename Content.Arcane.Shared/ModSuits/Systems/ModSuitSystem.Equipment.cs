using Content.Arcane.Shared.ModSuits.Components;
using Content.Goobstation.Shared.Clothing.Components;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    [Dependency] private SharedTemperatureSystem _temperature = default!;

    private readonly Dictionary<(EntityUid Target, Type Type), ComponentGrant> _equipmentGrants = [];
    private readonly Dictionary<EntityUid, List<ComponentGrant>> _moduleGrants = [];

    private sealed record ComponentGrant((EntityUid Target, Type Type) Key, IComponent Component)
    {
        public int Count = 1;
    }

    private void InitializeEquipmentModules()
    {
        SubscribeLocalEvent<ModGrantComponentsModuleComponent, ModModuleActivatedEvent>(OnEquipmentActivated);
        SubscribeLocalEvent<ModGrantComponentsModuleComponent, ModModuleDeactivatedEvent>(OnEquipmentDeactivated);
        SubscribeLocalEvent<ModGrantComponentsModuleComponent, EntityTerminatingEvent>(OnEquipmentTerminating);
    }

    private void OnEquipmentActivated(Entity<ModGrantComponentsModuleComponent> module, ref ModModuleActivatedEvent args)
    {
        if (TryComp(args.Suit, out ModSuitComponent? suit))
            RefreshEquipmentGrant((args.Suit, suit), module);
    }

    private void OnEquipmentDeactivated(Entity<ModGrantComponentsModuleComponent> module, ref ModModuleDeactivatedEvent args) =>
        RevokeEquipmentGrant(module);

    private void OnEquipmentTerminating(Entity<ModGrantComponentsModuleComponent> module, ref EntityTerminatingEvent args) =>
        RevokeEquipmentGrant(module);

    private void RefreshEquipmentGrants(Entity<ModSuitComponent> suit)
    {
        if (_net.IsClient || suit.Comp.ModulesContainer is not { } modules)
            return;

        foreach (var uid in modules.ContainedEntities)
        {
            if (TryComp(uid, out ModModuleComponent? active) && active.Active
                && TryComp(uid, out ModGrantComponentsModuleComponent? equipment))
                RefreshEquipmentGrant(suit, (uid, equipment));
        }
    }

    private void RefreshEquipmentGrant(Entity<ModSuitComponent> suit, Entity<ModGrantComponentsModuleComponent> module)
    {
        EntityUid? target = module.Comp.Target switch
        {
            ModGrantTarget.Suit => suit.Owner,
            ModGrantTarget.Wearer => suit.Comp.Wearer,
            _ => module.Comp.Slot is { } slot && suit.Comp.PartEntities.TryGetValue(slot, out var part) ? part : null,
        };

        if (module.Comp.GrantedTo == target)
            return;

        RevokeEquipmentGrant(module);
        if (target is not { } uid || TerminatingOrDeleted(uid))
            return;

        var missing = new ComponentRegistry();
        var claims = new List<ComponentGrant>();
        foreach (var (name, entry) in module.Comp.Components)
        {
            var type = entry.Component.GetType();
            var hasComponent = TryComp(uid, type, out var existing);
            if (_equipmentGrants.TryGetValue((uid, type), out var grant) && ReferenceEquals(grant.Component, existing))
            {
                grant.Count++;
                claims.Add(grant);
            }
            else if (!hasComponent)
                missing.Add(name, entry);
        }

        EntityManager.AddComponents(uid, missing, removeExisting: false);
        foreach (var entry in missing.Values)
        {
            var type = entry.Component.GetType();
            if (TryComp(uid, type, out var created))
            {
                if (created is ClothingAutoInjectComponent injector)
                    injector.NextAutoInjectTime = module.Comp.NextAutoInjection;
                var key = (uid, type);
                var grant = new ComponentGrant(key, created);
                _equipmentGrants[key] = grant;
                claims.Add(grant);
            }
        }

        _moduleGrants[module] = claims;
        module.Comp.GrantedTo = uid;
    }

    private void RevokeEquipmentGrant(Entity<ModGrantComponentsModuleComponent> module)
    {
        module.Comp.GrantedTo = null;
        if (!_moduleGrants.Remove(module, out var claims))
            return;

        foreach (var claim in claims)
        {
            if (claim.Component is ClothingAutoInjectComponent injector)
                module.Comp.NextAutoInjection = injector.NextAutoInjectTime;
            if (--claim.Count > 0 || !_equipmentGrants.TryGetValue(claim.Key, out var currentGrant)
                || !ReferenceEquals(currentGrant, claim))
                continue;

            _equipmentGrants.Remove(claim.Key);
            var (uid, type) = claim.Key;
            if (!TerminatingOrDeleted(uid) && TryComp(uid, type, out var current)
                && ReferenceEquals(current, claim.Component))
                RemComp(uid, current);
        }
    }

    private void UpdateThermalRegulators(float frameTime)
    {
        var query = EntityQueryEnumerator<ModThermalRegulatorModuleComponent, ModModuleComponent>();
        while (query.MoveNext(out _, out var regulator, out var module))
        {
            if (!module.Active || !TryComp(module.Suit, out ModSuitComponent? suit)
                || suit.Wearer is not { } wearer || !TryComp(wearer, out TemperatureComponent? temperature))
                continue;

            var limit = regulator.Rate * frameTime;
            var delta = Math.Clamp(regulator.TargetTemperature - temperature.Temperature, -limit, limit);
            _temperature.ChangeHeat((wearer, temperature), delta * temperature.HeatCapacity, ignoreHeatResistance: true);
        }
    }
}
