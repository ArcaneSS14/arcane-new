using Content.Arcane.Shared.Power;
using Content.Goobstation.Shared.Supermatter;
using Content.Goobstation.Shared.Supermatter.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Managers;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Radio;
using Content.Shared.Radiation.Systems;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Power;

public sealed partial class SupermatterEventsSystem : EntitySystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private SharedRadiationSystem _radiation = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly ProtoId<WeightedRandomPrototype> NormalEvents = "ArcaneSupermatterNormalEvents";
    private static readonly ProtoId<WeightedRandomPrototype> HarshEvents = "ArcaneSupermatterHarshEvents";
    private static readonly ProtoId<RadioChannelPrototype> Engineering = "Engineering";
    private static readonly (Gas Gas, float Weight)[] AngerWeights =
    [
        (Gas.Oxygen, 1f), (Gas.Nitrogen, -1f), (Gas.CarbonDioxide, 3f), (Gas.Plasma, 5f),
        (Gas.Tritium, 10f), (Gas.WaterVapor, 3f), (Gas.Frezon, -5f), (Gas.Ammonia, 1f),
        (Gas.NitrousOxide, -3f), (Gas.Nitrium, 10f), (Gas.BZ, 3f), (Gas.Healium, -5f),
        (Gas.Pluoxium, -3f),
    ];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SupermatterComponent, SupermatterGasSampleEvent>(OnGasSample);
        SubscribeLocalEvent<SupermatterEventsComponent, SupermatterCycleStartingEvent>(OnCycleStarting);
        SubscribeLocalEvent<SupermatterEventsComponent, SupermatterCycleCompletedEvent>(OnCycleCompleted);
        SubscribeLocalEvent<SupermatterEventsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<SupermatterComponent, ComponentShutdown>(OnCrystalShutdown);
    }

    private void OnGasSample(Entity<SupermatterComponent> ent, ref SupermatterGasSampleEvent args)
    {
        var events = EnsureComp<SupermatterEventsComponent>(ent);
        var anger = 0f;
        foreach (var (gas, weight) in AngerWeights)
        {
            var amount = args.Sample.GetMoles(gas);
            events.GasSample[(int) gas] = amount;
            anger += amount * weight;
        }
        events.GasSample[(int) Gas.Fungalin] = args.Sample.GetMoles(Gas.Fungalin);
        events.SampleMoles = args.TotalMoles;
        events.SampleTemperature = args.Sample.Temperature;
        events.WasteMultiplier = args.WasteMultiplier;

        events.LastAnger = anger / args.TotalMoles;
        events.Anger = Math.Max(events.Anger + events.LastAnger, 0f);
    }

    private void OnCycleStarting(Entity<SupermatterEventsComponent> ent, ref SupermatterCycleStartingEvent args)
    {
        if (ent.Comp.SurgeUntil == default || _timing.CurTime < ent.Comp.SurgeUntil)
            return;
        ent.Comp.SurgeUntil = default;
        EndSurge(ent.Owner);
    }

    private void OnCycleCompleted(Entity<SupermatterEventsComponent> ent, ref SupermatterCycleCompletedEvent args)
    {
        if (!TryComp<SupermatterComponent>(ent, out var sm) || TerminatingOrDeleted(ent) ||
            EntityManager.IsQueuedForDeletion(ent))
            return;

        if (!args.HasGasSample)
        {
            ent.Comp.SampleMoles = 0f;
            ent.Comp.SampleTemperature = 0f;
            ent.Comp.WasteMultiplier = 0f;
            return;
        }

        if (ent.Comp.Anger < ent.Comp.EventThreshold)
            return;

        var pool = ent.Comp.LastAnger >= ent.Comp.HarshThreshold ? HarshEvents : NormalEvents;
        var id = ProtoMan.Index(pool).Pick(_random);
        if (!ProtoMan.TryIndex<SupermatterEventPrototype>(id, out var effect))
            return;

        if (effect.GasToSpawn is { } gas)
        {
            if (_atmosphere.GetContainingMixture(ent.Owner, true, true) is not { Immutable: false } mixture)
                return;
            mixture.AdjustMoles(gas, 2000f);
        }
        else if (effect.ProtoToSpawn is { } prototype)
        {
            SpawnAtPosition(prototype, Transform(ent).Coordinates.Offset(new Vector2(0f, -1f)));
        }
        else if (effect.Surge)
        {
            sm.ArcaneSurgeActive = true;
            ent.Comp.SurgeUntil = _timing.CurTime + ent.Comp.SurgeDuration;
            _radiation.SetTransientIntensity(ent.Owner, sm.ArcaneTransmittedPower * sm.EffectiveRadiationOutputFactor);
        }

        ent.Comp.Anger = 0f;
        _radio.SendRadioMessage(ent, Loc.GetString(effect.Announcement), ProtoMan.Index(Engineering), ent);
        _chat.SendAdminAlert($"{effect.ID} triggered by supermatter {ToPrettyString(ent)}");
    }

    private void OnShutdown(Entity<SupermatterEventsComponent> ent, ref ComponentShutdown args)
        => EndSurge(ent.Owner);

    private void OnCrystalShutdown(Entity<SupermatterComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.ArcaneSurgeActive)
            _radiation.SetTransientIntensity(ent.Owner, null);
    }

    private void EndSurge(EntityUid uid)
    {
        if (!TryComp<SupermatterComponent>(uid, out var sm) || !sm.ArcaneSurgeActive)
            return;
        sm.ArcaneSurgeActive = false;
        _radiation.SetTransientIntensity(uid, null);
    }
}
