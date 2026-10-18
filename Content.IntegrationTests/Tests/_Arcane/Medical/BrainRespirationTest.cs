using Content.Arcane.Common.CCVar;
using Content.Arcane.Common.Medical.Respiration;
using Content.Arcane.Server.Medical.Respiration;
using Content.Arcane.Shared.Medical.Respiration;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Shared.Atmos;
using Content.Shared.Body;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Content.Shared.StatusEffectNew;
using Content.Trauma.Common.MartialArts;
using Content.Trauma.Common.Body;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane.Medical;

[TestOf(typeof(BrainRespirationSystem))]
public sealed class BrainRespirationTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly BrainRespirationSystem _respiration = null!;
    [SidedDependency(Side.Server)] private readonly BrainOxygenSystem _oxygen = null!;
    [SidedDependency(Side.Server)] private readonly BodySystem _body = null!;
    [SidedDependency(Side.Server)] private readonly StatusEffectsSystem _effects = null!;
    [SidedDependency(Side.Server)] private readonly DamageableSystem _damage = null!;
    [SidedDependency(Side.Server)] private readonly IConfigurationManager _cfg = null!;
    [SidedDependency(Side.Server)] private readonly MobStateSystem _mob = null!;

    [TestCase(Gas.Oxygen, false, true)]
    [TestCase(Gas.Nitrogen, false, false)]
    [TestCase(Gas.Oxygen, true, false)]
    public async Task InhaledGasRespectsMetabolismAndBlockedBreathing(Gas gasType, bool blocked, bool oxygenates)
    {
        var patient = await CreatePatient();
        await Server.WaitPost(() =>
        {
            Assert.That(_oxygen.GetBrain(patient), Is.Not.Null);
            var brain = _oxygen.GetBrain(patient)!.Value;
            _respiration.SetSaturation(brain, 0.5f);
            if (blocked)
                SEntMan.EnsureComponent<BlockedBreathingComponent>(patient);

            var gas = new GasMixture(6f);
            gas.AdjustMoles(gasType, 10f);
            var ev = new InhaledGasEvent(gas);
            SEntMan.EventBus.RaiseLocalEvent(patient, ref ev);
            Assert.That(ev.Handled && ev.Succeeded, Is.True);
            if (oxygenates)
                Assert.That(brain.Comp.AirSaturation, Is.GreaterThan(0.5f).And.LessThanOrEqualTo(1f));
            else
                Assert.That(brain.Comp.AirSaturation, Is.EqualTo(0.5f));
        });
    }

    [Test]
    public async Task HypoxiaUsesReservesAndCPRWhileDisablingRestoresTheOriginalModel()
    {
        var patient = await CreatePatient();
        var original = _cfg.GetCVar(ArcaneCVars.ComplexRespiratorEnabled);
        EntityUid brainUid = default;
        try
        {
            await Server.WaitPost(() =>
            {
                var brain = _oxygen.GetBrain(patient)!.Value;
                brainUid = brain.Owner;
                brain.Comp.AirConsumption = 0;
                SEntMan.EnsureComponent<BlockedBreathingComponent>(patient);
                SEntMan.EnsureComponent<SlowAccentComponent>(patient);
                _respiration.SetSaturation(brain, 0.85f);
            });
            await RunSeconds(4.2f);
            await Server.WaitPost(() =>
            {
                Assert.That(GetAsphyxiation(patient), Is.Zero);
                _respiration.SetSaturation((brainUid, SComp<BrainOxygenComponent>(brainUid)), 0.7f);
            });
            await RunSeconds(4.2f);
            await Server.WaitPost(() =>
            {
                var before = GetAsphyxiation(patient);
                Assert.That(before, Is.GreaterThan(0));
                Assert.That(_effects.TryGetStatusEffect(patient, "StatusEffectOxygenDeprived", out _), Is.True);
                Assert.That(_effects.TryGetStatusEffect(patient, "StatusEffectHypoxicSpeech", out _), Is.True);

                var completed = new CPRCompletedEvent();
                SEntMan.EventBus.RaiseLocalEvent(patient, ref completed);
                Assert.That(completed.AllowRevival, Is.False);
                Assert.That(GetAsphyxiation(patient), Is.LessThan(before));
                _cfg.SetCVar(ArcaneCVars.ComplexRespiratorEnabled, false);
            });
            await RunSeconds(0.2f);
            await Server.WaitPost(() =>
            {
                Assert.That(_oxygen.GetBrain(patient), Is.Null);
                Assert.That(SComp<BrainOxygenComponent>(brainUid).AirSaturation, Is.EqualTo(1f));
                Assert.That(_effects.TryGetStatusEffect(patient, "StatusEffectOxygenDeprived", out _), Is.False);
                Assert.That(_effects.TryGetStatusEffect(patient, "StatusEffectHypoxicSpeech", out _), Is.False);
                Assert.That(SHasComp<SlowAccentComponent>(patient), Is.True);
                var completed = new CPRCompletedEvent();
                SEntMan.EventBus.RaiseLocalEvent(patient, ref completed);
                Assert.That(completed.AllowRevival, Is.True);
                var suffocation = new SuffocationBeforeEvent();
                SEntMan.EventBus.RaiseLocalEvent(patient, ref suffocation);
                Assert.That(suffocation.Cancelled, Is.False);
            });
        }
        finally
        {
            await Server.WaitPost(() => _cfg.SetCVar(ArcaneCVars.ComplexRespiratorEnabled, original));
            await RunSeconds(0.2f);
        }
    }

    [Test]
    public async Task CancelledSuffocationRetainsGaspCooldown()
    {
        var patient = await CreatePatient();
        await RunSeconds(10.2f);
        await Server.WaitPost(() =>
        {
            var respirator = SComp<RespiratorComponent>(patient);
            Assert.That(GetAsphyxiation(patient), Is.Zero);
            Assert.That(respirator.LastGaspEmoteTime, Is.GreaterThan(TimeSpan.Zero));
            var firstGasp = respirator.LastGaspEmoteTime;
            var suffocation = new SuffocationBeforeEvent();
            SEntMan.EventBus.RaiseLocalEvent(patient, ref suffocation);
            Assert.That(suffocation.Cancelled, Is.True);
            Assert.That(respirator.LastGaspEmoteTime, Is.EqualTo(firstGasp));
        });
    }

    [Test]
    public async Task RemovingAndReinsertingTheBrainClearsOnlyItsOwnEffects()
    {
        var patient = await CreatePatient();
        EntityUid brainUid = default;
        await Server.WaitPost(() =>
        {
            var brain = _oxygen.GetBrain(patient)!.Value;
            brainUid = brain.Owner;
            _respiration.SetSaturation(brain, 0.7f);
            Assert.That(_effects.TryAddStatusEffectDuration(patient, "StatusEffectOpiated", TimeSpan.FromSeconds(60)), Is.True);
            Assert.That(_body.RemoveOrgan(patient, brainUid), Is.True);
        });
        await RunSeconds(0.2f);
        await Server.WaitPost(() =>
        {
            Assert.That(_oxygen.GetBrain(patient), Is.Null);
            Assert.That(_effects.TryGetStatusEffect(patient, "StatusEffectOxygenDeprived", out _), Is.False);
            Assert.That(_effects.TryGetStatusEffect(patient, "StatusEffectHypoxicSpeech", out _), Is.False);
            Assert.That(_effects.TryGetStatusEffect(patient, "StatusEffectOpiated", out _), Is.True);
            var completed = new CPRCompletedEvent();
            SEntMan.EventBus.RaiseLocalEvent(patient, ref completed);
            Assert.That(completed.AllowRevival, Is.False);
            Assert.That(_body.InsertOrgan(patient, brainUid), Is.True);
            Assert.That(_oxygen.GetBrain(patient), Is.Not.Null);
            Assert.That(_effects.TryGetStatusEffect(patient, "StatusEffectOxygenDeprived", out _), Is.True);
        });
    }

    [Test]
    public async Task DeadBodiesRetainOxygenWhileDetachedBrainsConsumeIt()
    {
        var patient = await CreatePatient();
        EntityUid brainUid = default;
        await Server.WaitPost(() =>
        {
            var brain = _oxygen.GetBrain(patient)!.Value;
            brainUid = brain.Owner;
            SEntMan.EnsureComponent<BlockedBreathingComponent>(patient);
            _respiration.SetSaturation(brain, 0.85f);
            _mob.ChangeMobState(patient, MobState.Dead);
        });
        await RunSeconds(8.2f);
        await Server.WaitPost(() =>
        {
            Assert.That(_mob.IsDead(patient), Is.True);
            Assert.That(SComp<BrainOxygenComponent>(brainUid).AirSaturation, Is.EqualTo(0.85f));
            Assert.That(_body.RemoveOrgan(patient, brainUid), Is.True);
        });
        await RunSeconds(4.2f);
        await Server.WaitPost(() =>
            Assert.That(SComp<BrainOxygenComponent>(brainUid).AirSaturation, Is.LessThan(0.85f)));
    }

    [Test]
    public async Task HypoxicSpeechRelaysWithoutAnAccentOnThePatientAndClearsOnRecovery()
    {
        var patient = await CreatePatient();
        await Server.WaitPost(() =>
        {
            Assert.That(SHasComp<SlowAccentComponent>(patient), Is.False);
            var brain = _oxygen.GetBrain(patient)!.Value;
            _respiration.SetSaturation(brain, 0.7f);

            var hypoxic = new AccentGetEvent(patient, "hello there");
            SEntMan.EventBus.RaiseLocalEvent(patient, ref hypoxic);
            Assert.That(hypoxic.Message, Is.EqualTo("hello... there..."));

            _respiration.SetSaturation(brain, 1f);
            var recovered = new AccentGetEvent(patient, "hello there");
            SEntMan.EventBus.RaiseLocalEvent(patient, ref recovered);
            Assert.That(recovered.Message, Is.EqualTo("hello there"));
            Assert.That(SHasComp<SlowAccentComponent>(patient), Is.False);
        });
    }

    private float GetAsphyxiation(EntityUid patient)
    {
        return _damage.GetAllDamage(patient).DamageDict.GetValueOrDefault("Asphyxiation").Float();
    }

    private async Task<EntityUid> CreatePatient()
    {
        await Pair.CreateTestMap();
        EntityUid patient = default;
        await Server.WaitPost(() => patient = SSpawnAtPosition("MobHuman", TestMap!.GridCoords));
        await RunSeconds(0.2f);
        return patient;
    }
}
