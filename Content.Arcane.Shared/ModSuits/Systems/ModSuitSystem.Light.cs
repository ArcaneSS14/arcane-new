using Content.Arcane.Shared.ModSuits.Components;
using Robust.Shared.Map;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    [Dependency] private SharedPointLightSystem _pointLights = default!;

    private void InitializeLights()
    {
        SubscribeLocalEvent<ModLightModuleComponent, ModModuleActivateAttemptEvent>(OnLightActivationAttempt);
        SubscribeLocalEvent<ModLightModuleComponent, ModModuleActivatedEvent>(OnLightActivated);
        SubscribeLocalEvent<ModLightModuleComponent, ModModuleDeactivatedEvent>(OnLightDeactivated);
        SubscribeLocalEvent<ModLightModuleComponent, EntityTerminatingEvent>(OnLightModuleTerminating);
    }

    private void OnLightActivationAttempt(Entity<ModLightModuleComponent> module, ref ModModuleActivateAttemptEvent args)
    {
        if (GetLightMount(args.Suit, module.Comp.Slot) == null)
            args.Cancelled = true;
    }

    private EntityUid? GetLightMount(EntityUid suit, string slot) =>
        TryComp(suit, out ModSuitComponent? control) && control.PartEntities.TryGetValue(slot, out var part)
        && !TerminatingOrDeleted(part) && IsPartDeployed((suit, control), slot)
            ? part
            : null;

    private void OnLightActivated(Entity<ModLightModuleComponent> module, ref ModModuleActivatedEvent args)
    {
        if (module.Comp.Light != null || GetLightMount(args.Suit, module.Comp.Slot) is not { } mount)
            return;

        // The module and the helmet's own flashlight can operate independently.
        var light = SpawnAttachedTo(module.Comp.LightPrototype, new EntityCoordinates(mount, Vector2.Zero));
        module.Comp.Light = light;
        _pointLights.SetRadius(light, module.Comp.Radius);
        _pointLights.SetEnergy(light, module.Comp.Energy);
        _pointLights.SetColor(light, module.Comp.Color);
    }

    private void OnLightDeactivated(Entity<ModLightModuleComponent> module, ref ModModuleDeactivatedEvent args) =>
        RemoveModuleLight(module);

    private void OnLightModuleTerminating(Entity<ModLightModuleComponent> module, ref EntityTerminatingEvent args) =>
        RemoveModuleLight(module);

    private void RemoveModuleLight(Entity<ModLightModuleComponent> module)
    {
        if (module.Comp.Light is not { } light)
            return;

        module.Comp.Light = null;
        if (!TerminatingOrDeleted(light))
        {
            _pointLights.SetEnabled(light, false);
            QueueDel(light);
        }
    }
}
