using Content.Arcane.Shared.AlertLevel;
using Content.Server.Light.Components;
using Content.Shared.AlertLevel;
using Content.Shared.Station.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.AlertLevel;

/// <summary>
/// Plays the alert level's alarm through the emergency lights of a station.
/// </summary>
public sealed partial class EmergencyLightAlarmSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(1);

    private readonly Dictionary<EntityUid, (AlertLevelAlarmPrototype Alarm, TimeSpan Next)> _stations = new();
    private readonly List<EntityUid> _due = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged);
    }

    private void OnAlertLevelChanged(ref AlertLevelChangedEvent ev)
    {
        if (!_proto.TryIndex<AlertLevelAlarmPrototype>(ev.AlertLevel.Id, out var alarm))
        {
            _stations.Remove(ev.Station);
            return;
        }

        _stations[ev.Station] = (alarm, _timing.CurTime + Interval(alarm));
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_stations.Count == 0)
            return;

        var now = _timing.CurTime;
        _due.Clear();
        foreach (var (station, (_, next)) in _stations)
        {
            if (now >= next)
                _due.Add(station);
        }

        foreach (var station in _due)
        {
            if (TerminatingOrDeleted(station))
            {
                _stations.Remove(station);
                continue;
            }

            var alarm = _stations[station].Alarm;
            _stations[station] = (alarm, now + Interval(alarm));
            PlayOnStation(station, alarm);
        }
    }

    private void PlayOnStation(EntityUid station, AlertLevelAlarmPrototype alarm)
    {
        var query = EntityQueryEnumerator<EmergencyLightComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var light, out var xform))
        {
            if (light.State == EmergencyLightState.Empty
                || CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != station)
                continue;

            _audio.PlayPvs(alarm.Sound, uid);
        }
    }

    private static TimeSpan Interval(AlertLevelAlarmPrototype alarm)
    {
        return alarm.Interval < MinInterval ? MinInterval : alarm.Interval;
    }
}
