#nullable enable

using System.Linq;
using Content.Arcane.Server.Atmos;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Medical.Shared.Wounds;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane.Atmos;

[TestOf(typeof(PressureDamageSystem))]
public sealed class PressureDamageTest : GameTest
{
    private static readonly EntProtoId BluntWound = "Blunt";

    public override PoolSettings PoolSettings => new() { Connected = false };

    [SidedDependency(Side.Server)] private readonly DamageableSystem _damage = null!;
    [SidedDependency(Side.Server)] private readonly BarotraumaSystem _pressure = null!;
    [SidedDependency(Side.Server)] private readonly BodySystem _body = null!;
    [SidedDependency(Side.Server)] private readonly WoundSystem _wounds = null!;
    [SidedDependency(Side.Server)] private readonly BloodstreamSystem _blood = null!;
    [SidedDependency(Side.Server)] private readonly MobThresholdSystem _thresholds = null!;
    [SidedDependency(Side.Server)] private readonly MobStateSystem _mobState = null!;

    [TestPrototypes]
    private const string Prototypes = """
- type: damageModifierSet
  id: TestArcanePressureBleeding
  coefficients:
    Blunt: 25

- type: entity
  id: TestArcanePressureBlood
  parent: [MobDamageable, MobBloodstream]
  components:
  - type: Barotrauma
    damage:
      types:
        Blunt: 0.6
  - type: Bloodstream
    damageBleedModifiers: TestArcanePressureBleeding
""";

    [TestCase(0f, 2f)]
    [TestCase(5f, 1.5f)]
    [TestCase(10f, 1f)]
    [TestCase(15f, 0.5f)]
    [TestCase(20f, 0f)]
    [RunOnSide(Side.Server)]
    public void PressureCurveRampsAndResets(float pressure, float initialDamage)
    {
        var patient = SSpawn("TestArcanePressureBlood");
        Assert.That(Sample(patient, pressure).GetTotal().Float(), Is.EqualTo(initialDamage).Within(0.01f));
        for (var i = 1; i < 60; i++)
            Sample(patient, pressure);
        Assert.That(Sample(patient, pressure).GetTotal().Float(), Is.EqualTo(initialDamage * 2.5f).Within(0.02f));

        Sample(patient, 101.325f);
        Assert.That(SEntMan.HasComponent<PressureExposureComponent>(patient), Is.False);
        Assert.That(Sample(patient, 0f).GetTotal().Float(), Is.EqualTo(2f));
        Sample(patient, 101.325f);
        Assert.That(Sample(patient, 0f, 2f).GetTotal().Float(), Is.EqualTo(4f));
        Assert.That(Sample(patient, 825f).GetTotal().Float(), Is.EqualTo(1f));
        Assert.That(Sample(patient, 1100f).GetTotal().Float(), Is.EqualTo(2f));
        Assert.That(Sample(patient, 550f).GetTotal().Float(), Is.Zero);
    }

    [Test]
    public async Task DeadPatientStopsAtDamageLimitWithoutBleeding()
    {
        await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var patient = SSpawnAtPosition("TestArcanePressureBlood", TestMap!.GridCoords);
            var initialBlood = _blood.GetBloodLevel(patient);
            var seed = new DamageSpecifier { DamageDict = { ["Blunt"] = 199 } };
            seed.Flags = DamageSpecifier.DamageFlags.NoWounds | DamageSpecifier.DamageFlags.NoBleeding;
            _damage.ChangeDamage(patient, seed, ignoreResistances: true);
            _mobState.ChangeMobState(patient, MobState.Dead);
            var barotrauma = SComp<BarotraumaComponent>(patient);
            barotrauma.LowPressureModifier = 20f;
            _pressure.Update(1f);
            Assert.That(_damage.GetAllDamage(patient).GetTotal().Float(), Is.EqualTo(199f));
            Assert.That(barotrauma.TakingDamage, Is.False);

            barotrauma.LowPressureModifier = 0f;
            _pressure.Update(1f);
            Assert.That(_damage.GetAllDamage(patient).GetTotal().Float(), Is.EqualTo(200f));
            for (var i = 0; i < 65; i++)
                _pressure.Update(1f);
            Assert.That(_damage.GetAllDamage(patient).GetTotal().Float(), Is.EqualTo(200f));
            Assert.That(_blood.GetBloodLevel(patient), Is.EqualTo(initialBlood));
            Assert.That(barotrauma.TakingDamage, Is.False);

            barotrauma.LowPressureModifier = 101.325f;
            _pressure.Update(1f);
            Assert.That(SEntMan.HasComponent<PressureExposureComponent>(patient), Is.False);

            barotrauma.LowPressureModifier = 0f;
            _damage.ChangeDamage(patient, new() { DamageDict = { ["Blunt"] = -10 } }, ignoreResistances: true);
            _pressure.Update(1f);
            Assert.That(_damage.GetAllDamage(patient).GetTotal().Float(), Is.EqualTo(192f).Within(0.02f));
            _damage.ChangeDamage(patient, new() { DamageDict = { ["Blunt"] = 1 } }, ignoreResistances: true);
            Assert.That(_blood.GetBloodLevel(patient), Is.LessThan(initialBlood), "Ordinary damage still causes blood loss.");
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void RemovingBarotraumaResetsExposure()
    {
        var patient = SSpawn("TestArcanePressureBlood");
        Sample(patient, 0f, 60f);
        Assert.That(Sample(patient, 0f).GetTotal().Float(), Is.EqualTo(5f).Within(0.02f));
        var damage = SComp<BarotraumaComponent>(patient).Damage;
        SEntMan.RemoveComponent<BarotraumaComponent>(patient);
        Assert.That(SEntMan.HasComponent<PressureExposureComponent>(patient), Is.False);

        SEntMan.AddComponent<BarotraumaComponent>(patient).Damage = damage;
        Assert.That(Sample(patient, 0f).GetTotal().Float(), Is.EqualTo(2f));
    }

    [Test]
    public async Task VacuumDoesNotCreateOrChangeWounds()
    {
        await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var patient = SSpawnAtPosition("MobHuman", TestMap!.GridCoords);
            var torso = _body.GetOrgan(patient, "Torso")!.Value;
            var part = new Entity<WoundableComponent?>(torso, SComp<WoundableComponent>(torso));
            Assert.That(_wounds.TryInduceWound(part, BluntWound, 10, out _), Is.True);
            var parts = _body.GetExternalOrgans(patient).ToArray();
            var initialBlood = _blood.GetBloodLevel(patient);
            _pressure.Update(1f);
            Assert.That(_thresholds.CheckVitalDamage(patient).Float(), Is.EqualTo(2f).Within(0.02f));
            var wounds = parts.SelectMany(p => _wounds.GetWoundableWounds((p.Owner, SComp<WoundableComponent>(p)))).ToArray();
            Assert.That(wounds.Length, Is.EqualTo(1));
            Assert.That(wounds[0].Comp.WoundSeverityPoint.Float(), Is.EqualTo(10f));
            Assert.That(_blood.GetBloodLevel(patient), Is.EqualTo(initialBlood));
            Assert.That(_body.GetExternalOrgans(patient).Count(), Is.EqualTo(parts.Length));

            _damage.ChangeDamage(torso, new() { DamageDict = { ["Blunt"] = 5 } }, ignoreResistances: true);
            Assert.That(_wounds.GetWoundableWounds(part).Single().Comp.WoundSeverityPoint.Float(), Is.EqualTo(15f));
        });
    }

    [Test]
    public async Task PressureLimitStopsDamageAfterDeath()
    {
        await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var patient = SSpawnAtPosition("MobHuman", TestMap!.GridCoords);
            var parts = _body.GetExternalOrgans(patient).ToArray();
            for (var i = 0; i < 120; i++)
                _pressure.Update(1f);

            Assert.That(SEntMan.Deleted(patient) || SEntMan.IsQueuedForDeletion(patient), Is.False);
            Assert.That(_mobState.IsDead(patient), Is.True);
            var stoppedDamage = _damage.GetAllDamage(patient).GetTotal();
            Assert.That(stoppedDamage.Float(), Is.GreaterThan(200f));
            Assert.That(_body.GetExternalOrgans(patient).Count(), Is.EqualTo(parts.Length));
            Assert.That(parts.All(part => !SEntMan.Deleted(part) && !SEntMan.IsQueuedForDeletion(part)), Is.True);
            for (var i = 0; i < 5; i++)
                _pressure.Update(1f);
            Assert.That(_damage.GetAllDamage(patient).GetTotal(), Is.EqualTo(stoppedDamage));
        });
    }

    private DamageSpecifier Sample(EntityUid patient, float pressure, float interval = 1f)
    {
        var args = new BarotraumaDamageEvent(pressure, interval, FixedPoint2.Zero);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref args);
        return args.Damage!;
    }
}
