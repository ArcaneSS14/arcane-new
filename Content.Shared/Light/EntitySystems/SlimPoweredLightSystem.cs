using Content.Shared.Light.Components;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;

namespace Content.Shared.Light.EntitySystems;

public sealed partial class SlimPoweredLightSystem : EntitySystem
{
    [Dependency] private SharedPowerReceiverSystem _receiver = default!;
    [Dependency] private SharedPointLightSystem _lights = default!;

    private bool _setting;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SlimPoweredLightComponent, AttemptPointLightToggleEvent>(OnLightAttempt);
        SubscribeLocalEvent<SlimPoweredLightComponent, PowerChangedEvent>(OnLightPowerChanged);
    }

    private void OnLightAttempt(Entity<SlimPoweredLightComponent> ent, ref AttemptPointLightToggleEvent args)
    {
        // Early-out to avoid having to trycomp stuff if we're the caller setting it
        if (_setting)
            return;

        if (args.Enabled && !_receiver.IsPowered(ent.Owner))
            args.Cancelled = true;
    }

    private void OnLightPowerChanged(Entity<SlimPoweredLightComponent> ent, ref PowerChangedEvent args)
    {
        if (ent.Comp.Enabled || ent.Comp.AiIllumination) // Arcane
            RefreshLight(ent, args.Powered); // Arcane
    }

    public void SetEnabled(Entity<SlimPoweredLightComponent?> entity, bool enabled)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (entity.Comp.Enabled == enabled)
            return;

        entity.Comp.Enabled = enabled;
        Dirty(entity);
        RefreshLight((entity.Owner, entity.Comp), _receiver.IsPowered(entity.Owner)); // Arcane
    }

    // Arcane-Start: automatic illumination does not own the manual switch.
    public void SetAiIllumination(Entity<SlimPoweredLightComponent?> entity, bool enabled)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false) || entity.Comp.AiIllumination == enabled)
            return;

        entity.Comp.AiIllumination = enabled;
        RefreshLight((entity.Owner, entity.Comp), _receiver.IsPowered(entity.Owner));
    }

    private void RefreshLight(Entity<SlimPoweredLightComponent> entity, bool powered)
    {
        if (!_lights.TryGetLight(entity.Owner, out var light))
            return;

        _setting = true;
        _lights.SetEnabled(entity.Owner, powered && (entity.Comp.Enabled || entity.Comp.AiIllumination), light);
        _setting = false;
    }
    // Arcane-End
}
