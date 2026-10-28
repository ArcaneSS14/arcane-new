// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Medical.CrewMonitoring;
using Content.Server.Power.EntitySystems;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Bed.Components;
using Content.Shared.Morgue.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Medical.CrewMonitoring;

public sealed partial class CrewMonitoringAlertSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private CrewMonitoringConsoleSystem _console = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private PointLightSystem _light = default!;
    [Dependency] private PowerReceiverSystem _power = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<CrewMonitoringAlertComponent, CrewMonitoringConsoleComponent>();
        while (query.MoveNext(out var uid, out var alert, out var console))
        {
            if (now < alert.NextAlert)
                continue;

            alert.NextAlert = now + alert.AlertInterval;

            if (_power.IsPowered(uid) && HasUnsecuredCorpse(console))
                StartAlert((uid, alert));
            else
                StopAlert((uid, alert));
        }
    }

    private void StartAlert(Entity<CrewMonitoringAlertComponent> ent)
    {
        if (TryComp<PointLightComponent>(ent, out var light))
        {
            ent.Comp.NormalLight ??= (light.Color, light.Energy, light.Radius);
            _light.SetColor(ent, ent.Comp.AlertLightColor, light);
            _light.SetEnergy(ent, ent.Comp.AlertLightEnergy, light);
            _light.SetRadius(ent, ent.Comp.AlertLightRadius, light);
        }

        _audio.PlayPvs(ent.Comp.AlertSound, ent);
    }

    private void StopAlert(Entity<CrewMonitoringAlertComponent> ent)
    {
        if (ent.Comp.NormalLight is not { } normal)
            return;

        ent.Comp.NormalLight = null;
        if (!TryComp<PointLightComponent>(ent, out var light))
            return;

        _light.SetColor(ent, normal.Color, light);
        _light.SetEnergy(ent, normal.Energy, light);
        _light.SetRadius(ent, normal.Radius, light);
    }

    private bool HasUnsecuredCorpse(CrewMonitoringConsoleComponent console)
    {
        foreach (var sensor in _console.GetVisibleSensors(console))
        {
            // coordinates are only sent by sensors set to tracking
            if (sensor.IsAlive || sensor.Coordinates == null)
                continue;

            if (TryGetEntity(sensor.OwnerUid, out var corpse) && !Deleted(corpse) && !IsSecured(corpse.Value))
                return true;
        }

        return false;
    }

    private bool IsSecured(EntityUid corpse)
    {
        if (_container.TryGetContainingContainer(corpse, out var container)
            && (HasComp<MorgueComponent>(container.Owner) || HasComp<AntiRottingContainerComponent>(container.Owner)))
            return true;

        return HasComp<StasisBedBuckledComponent>(corpse);
    }
}
