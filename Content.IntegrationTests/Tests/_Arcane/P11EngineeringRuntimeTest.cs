// Arcane-Start
#nullable enable
using Content.Arcane.Server.Power;
using Content.Arcane.Shared.Power;
using Content.Goobstation.Server.Supermatter.Systems;
using Content.Goobstation.Shared.Supermatter;
using Content.Goobstation.Shared.Supermatter.Components;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Projectiles;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Singularity.Components;
using Content.Trauma.Shared.Construction;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P11EngineeringRuntimeTest : InteractionTest
{
    private static readonly ProtoId<WeightedRandomPrototype> NormalEvents = "ArcaneSupermatterNormalEvents";

    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [Test]
    public async Task EmitterDiscRequiresHeldItemAndCannotUpgradeTwice()
    {
        await SpawnTarget("Emitter");
        var emitter = STarget!.Value;
        await InteractUsing("DiodeDiscElectroDisruptive", awaitDoAfters: false);
        await DropAll();
        await RunSeconds(2.3f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<UpgradedMachineComponent>(emitter), Is.False);
            Assert.That(SEntMan.GetComponent<EmitterComponent>(emitter).BoltType.Id, Is.EqualTo("EmitterBolt"));
        });

        var disc = await PlaceInHands("DiodeDiscElectroBehavioural");
        var discUid = ToServer(disc);
        await Interact();
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<UpgradedMachineComponent>(emitter), Is.True);
            Assert.That(SEntMan.GetComponent<EmitterComponent>(emitter).BoltType.Id, Is.EqualTo("EmitterBoltElectroBehavioural"));
            Assert.That(SEntMan.EntityExists(discUid), Is.False);
        });

        var spare = ToServer(await PlaceInHands("DiodeDiscElectroDisruptive"));
        await Interact();
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(spare), Is.True);
            Assert.That(SEntMan.GetComponent<EmitterComponent>(emitter).BoltType.Id, Is.EqualTo("EmitterBoltElectroBehavioural"));
        });
    }

    [Test]
    public async Task SupermatterKeepsAccumulatedDamageAndAppliesEachDiodeOnce()
    {
        await AddAtmosphere();
        await SetTile(Plating, TargetCoords, MapData.Grid);
        await SpawnTarget("Supermatter");
        var crystal = STarget!.Value;
        await Server.WaitAssertion(() =>
        {
            var atmosphere = SEntMan.System<AtmosphereSystem>();
            var mix = atmosphere.GetTileMixture(crystal, true);
            Assert.That(mix, Is.Not.Null);
            Assert.That(mix!.TotalMoles, Is.GreaterThan(0f));
            var nitrogenRatio = mix.GetMoles(Gas.Nitrogen) / mix.TotalMoles;

            var sm = SEntMan.GetComponent<SupermatterComponent>(crystal);
            sm.Damage = 50f;
            SEntMan.System<SupermatterSystem>().Cycle(crystal, sm);
            Assert.That(sm.Damage, Is.GreaterThan(40f), "The gas cycle overwrote accumulated damage.");
            var reading = SEntMan.GetComponent<SupermatterEventsComponent>(crystal);
            Assert.That(reading.SampleMoles, Is.GreaterThan(0f));
            Assert.That(reading.GasSample[(int) Gas.Nitrogen] / reading.SampleMoles, Is.EqualTo(nitrogenRatio).Within(0.001f));

            var before = sm.Damage;
            var power = sm.Power;
            var bolt = SEntMan.SpawnAtPosition("EmitterBoltElectroBehavioural", Position(crystal));
            var projectile = SEntMan.GetComponent<ProjectileComponent>(bolt);
            var hit = new ProjectileHitEvent(projectile.Damage, crystal);
            SEntMan.EventBus.RaiseLocalEvent(bolt, ref hit);
            SEntMan.EventBus.RaiseLocalEvent(bolt, ref hit);
            Assert.That(sm.Damage, Is.EqualTo(before + 1f).Within(0.001f));
            Assert.That(sm.Power, Is.EqualTo(power + 100f + (float) projectile.Damage.GetTotal()).Within(0.001f));
            SEntMan.DeleteEntity(bolt);

            sm.Activated = false;
            var pool = Server.ResolveDependency<IPrototypeManager>().Index(NormalEvents);
            var sampleRandom = new RobustRandom();
            var seed = 0;
            while (seed < 100)
            {
                sampleRandom.SetSeed(seed);
                if (pool.Pick(sampleRandom) == "SMSurge")
                    break;
                seed++;
            }
            Assert.That(seed, Is.LessThan(100), "The normal event table never selected a surge.");
            reading.Anger = reading.EventThreshold;
            reading.LastAnger = 0f;
            reading.SurgeDuration = TimeSpan.FromSeconds(0.2);
            Server.ResolveDependency<IRobustRandom>().SetSeed(seed);
            var cycle = new SupermatterCycleCompletedEvent(true);
            SEntMan.EventBus.RaiseLocalEvent(crystal, ref cycle);
            Assert.That(sm.ArcaneSurgeActive, Is.True);
            Assert.That(sm.EffectiveGasEfficiency, Is.EqualTo(0.30f));
            var radiation = SEntMan.System<SharedRadiationSystem>();
            radiation.SetIntensity(crystal, 4f);
            radiation.SetTransientIntensity(crystal, 12f);
            Assert.That(SEntMan.GetComponent<RadiationSourceComponent>(crystal).EffectiveIntensity, Is.EqualTo(12f));
        });
        await RunSeconds(0.3f);
        await Server.WaitAssertion(() =>
        {
            // The start of a new cycle must clean up even before it can take a gas sample.
            var cycle = new SupermatterCycleStartingEvent();
            SEntMan.EventBus.RaiseLocalEvent(crystal, ref cycle);
            var sm = SEntMan.GetComponent<SupermatterComponent>(crystal);
            Assert.That(sm.ArcaneSurgeActive, Is.False);
            Assert.That(sm.EffectiveGasEfficiency, Is.EqualTo(sm.GasEfficiency));
            Assert.That(SEntMan.GetComponent<RadiationSourceComponent>(crystal).EffectiveIntensity, Is.EqualTo(4f));
        });
    }

    [Test]
    public async Task HandheldMonitorListsAnAnchoredCrystalOnTheCurrentGrid()
    {
        await SetTile(Plating, TargetCoords, MapData.Grid);
        await SpawnTarget("Supermatter");
        var crystal = Target!.Value;
        var monitor = await PlaceInHands("BaseHandheldStationMapSupermatterCe");
        await DropAll();
        await Server.WaitAssertion(() =>
        {
            var item = ToServer(monitor);
            SUiSys.OpenUi(item, SupermatterMonitorUiKey.Key, SPlayer);
            Assert.That(SUiSys.IsUiOpen(item, SupermatterMonitorUiKey.Key, SPlayer), Is.False);
        });
        await Pickup(monitor);
        await Server.WaitAssertion(() =>
        {
            var item = ToServer(monitor);
            Assert.That(SEntMan.System<SharedHandsSystem>().IsHolding(SPlayer, item), Is.True);
            SUiSys.OpenUi(item, SupermatterMonitorUiKey.Key, SPlayer);
            Assert.That(SUiSys.TryGetUiState<SupermatterMonitorState>(item, SupermatterMonitorUiKey.Key, out var state), Is.True);
            Assert.That(state!.Crystals, Has.Length.EqualTo(1));
            Assert.That(state.Crystals[0].Crystal, Is.EqualTo(crystal));
            Assert.That(state.Crystals[0].Status, Is.EqualTo(SupermatterMonitorStatus.Inactive));
            Assert.That(state.Reading?.Integrity, Is.EqualTo(100f));
        });
        await RunTicks(10);
        await Server.WaitAssertion(() => SEntMan.DeleteEntity(ToServer(crystal)));
        await RunSeconds(1.2f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SUiSys.TryGetUiState<SupermatterMonitorState>(ToServer(monitor), SupermatterMonitorUiKey.Key, out var state), Is.True);
            Assert.That(state!.Crystals, Is.Empty);
            Assert.That(state.Reading, Is.Null);
        });
    }
}
// Arcane-End
