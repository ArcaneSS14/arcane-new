using Content.Arcane.Shared.Species.Hydrakin;
using Content.Server.Popups;
using Content.Server.Temperature.Systems;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Systems;
using Content.Shared.Temperature.Components;
using Robust.Shared.Audio.Systems;

namespace Content.Arcane.Server.Species.Hydrakin;

public sealed partial class HydrakinSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private PopupSystem _popups = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private TemperatureSystem _temperature = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (frameTime <= 0f)
            return;

        var query = EntityQueryEnumerator<HydrakinComponent, TemperatureComponent>();
        while (query.MoveNext(out var uid, out var hydrakin, out var temperature))
        {
            if (_mobState.IsDead(uid) ||
                temperature.Temperature <= hydrakin.MinimumTemperature ||
                temperature.Temperature >= hydrakin.MaximumTemperature)
            {
                continue;
            }

            var multiplier = _mobState.IsCritical(uid) ? hydrakin.CriticalHeatMultiplier : 1f;
            _temperature.ChangeHeat((uid, temperature), hydrakin.PassiveHeatWatts * frameTime * multiplier, true);
        }
    }

    [SubscribeLocalEvent]
    private void OnStartup(EntityUid uid, HydrakinComponent component, ComponentStartup args)
    {
        _actions.AddAction(uid, ref component.CoolOffAction, component.CoolOffActionId.Id);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(EntityUid uid, HydrakinComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.CoolOffAction);
    }

    [SubscribeLocalEvent]
    private void OnCoolOff(EntityUid uid, HydrakinComponent component, HydrakinCoolOffActionEvent args)
    {
        var doAfter = new DoAfterArgs(
            EntityManager,
            uid,
            TimeSpan.FromSeconds(3),
            new HydrakinCoolOffDoAfterEvent(),
            uid)
        {
            NeedHand = false,
            RequireCanInteract = false,
            BreakOnHandChange = false,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
            args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnCoolOffDoAfter(Entity<HydrakinComponent> ent, ref HydrakinCoolOffDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        _popups.PopupEntity(
            Loc.GetString("hydrakin-cool-off-emote", ("name", Identity.Entity(ent, EntityManager))),
            ent);
        _audio.PlayEntity(ent.Comp.CoolOffSound, ent, ent);
        args.Handled = true;

        if (!TryComp<TemperatureComponent>(ent, out var temperature))
            return;

        var temperatureChange = -(ent.Comp.CoolOffCoefficient * temperature.Temperature);
        var heatChange = temperature.HeatCapacity * temperatureChange;
        _temperature.ChangeHeat((ent, temperature), heatChange, true);
    }
}
