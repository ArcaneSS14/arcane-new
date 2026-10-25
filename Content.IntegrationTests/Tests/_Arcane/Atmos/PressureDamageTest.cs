#nullable enable

using System.Linq;
using Content.Arcane.Server.Atmos;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Medical.Shared.Wounds;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
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
    [SidedDependency(Side.Server)] private readonly InventorySystem _inventory = null!;

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

    [Test]
    public async Task DeadPatientStopsAtDamageLimitWithoutBleeding()
    {
        await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var patient = SSpawnAtPosition("TestArcanePressureBlood", TestMap!.GridCoords);
            var initialBlood = _blood.GetBloodLevel(patient);
            var barotrauma = SComp<BarotraumaComponent>(patient);
            var limit = barotrauma.MaxDamage;
            var seed = new DamageSpecifier { DamageDict = { ["Blunt"] = limit - 1 } };
            seed.Flags = DamageSpecifier.DamageFlags.NoWounds | DamageSpecifier.DamageFlags.NoBleeding;
            _damage.ChangeDamage(patient, seed, ignoreResistances: true);
            _mobState.ChangeMobState(patient, MobState.Dead);
            barotrauma.LowPressureModifier = Atmospherics.HazardLowPressure;
            _pressure.Update(1f);
            Assert.That(_damage.GetAllDamage(patient).GetTotal(), Is.EqualTo(limit - 1));
            Assert.That(barotrauma.TakingDamage, Is.False);

            barotrauma.LowPressureModifier = 0f;
            _pressure.Update(1f);
            Assert.That(_damage.GetAllDamage(patient).GetTotal(), Is.EqualTo(limit));
            _pressure.Update(1f);
            Assert.That(_damage.GetAllDamage(patient).GetTotal(), Is.EqualTo(limit));
            Assert.That(_blood.GetBloodLevel(patient), Is.EqualTo(initialBlood));
            Assert.That(barotrauma.TakingDamage, Is.False);

            barotrauma.LowPressureModifier = Atmospherics.OneAtmosphere;
            _pressure.Update(1f);
            Assert.That(SEntMan.HasComponent<PressureExposureComponent>(patient), Is.False);

            barotrauma.LowPressureModifier = 0f;
            _damage.ChangeDamage(patient, new() { DamageDict = { ["Blunt"] = -10 } }, ignoreResistances: true);
            var afterHealing = _damage.GetAllDamage(patient).GetTotal();
            _pressure.Update(1f);
            Assert.That(_damage.GetAllDamage(patient).GetTotal(), Is.GreaterThan(afterHealing).And.LessThanOrEqualTo(limit));
            _damage.ChangeDamage(patient, new() { DamageDict = { ["Blunt"] = 1 } }, ignoreResistances: true);
            Assert.That(_blood.GetBloodLevel(patient), Is.LessThan(initialBlood), "Ordinary damage still causes blood loss.");
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void RemovingBarotraumaResetsExposure()
    {
        var patient = SSpawn("TestArcanePressureBlood");
        var initialDamage = Sample(patient, 0f).GetTotal();
        Sample(patient, 0f, 60f);
        Assert.That(Sample(patient, 0f).GetTotal(), Is.GreaterThan(initialDamage));
        var damage = SComp<BarotraumaComponent>(patient).Damage;
        SEntMan.RemoveComponent<BarotraumaComponent>(patient);
        Assert.That(SEntMan.HasComponent<PressureExposureComponent>(patient), Is.False);

        SEntMan.AddComponent<BarotraumaComponent>(patient).Damage = damage;
        Assert.That(Sample(patient, 0f).GetTotal(), Is.EqualTo(initialDamage));
        Assert.That(Sample(patient, Atmospherics.HazardLowPressure).GetTotal().Float(), Is.Zero);
        Assert.That(SEntMan.HasComponent<PressureExposureComponent>(patient), Is.False);
        Assert.That(Sample(patient, Atmospherics.HazardHighPressure).GetTotal().Float(), Is.Zero);
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
            var initialDamage = _damage.GetAllDamage(patient).GetTotal();
            Assert.That(initialDamage, Is.GreaterThan(FixedPoint2.Zero));

            var suit = SSpawn("ClothingOuterHardsuitEngineering");
            var helmet = SSpawn("ClothingHeadHelmetHardsuitEngineering");
            Assert.That(_inventory.TryEquip(patient, suit, "outerClothing", force: true), Is.True);
            Assert.That(_inventory.TryEquip(patient, helmet, "head", force: true), Is.True);
            var barotrauma = SComp<BarotraumaComponent>(patient);
            Assert.That(_pressure.GetFeltLowPressure(patient, barotrauma, 0f),
                Is.GreaterThanOrEqualTo(Atmospherics.HazardLowPressure));
            _pressure.Update(1f);
            Assert.That(_damage.GetAllDamage(patient).GetTotal(), Is.EqualTo(initialDamage));
            Assert.That(SEntMan.HasComponent<PressureExposureComponent>(patient), Is.False);
            Assert.That(barotrauma.TakingDamage, Is.False);

            Assert.That(_inventory.TryUnequip(patient, "head", force: true), Is.True);
            Assert.That(_inventory.TryUnequip(patient, "outerClothing", force: true), Is.True);
            _pressure.Update(1f);
            Assert.That(_damage.GetAllDamage(patient).GetTotal() - initialDamage, Is.EqualTo(initialDamage),
                "Removing pressure protection must restart damage at its initial rate.");

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
            var limit = SComp<BarotraumaComponent>(patient).MaxDamage;
            Assert.That(_thresholds.TryGetDeadThreshold(patient, out var deathThreshold), Is.True);
            _thresholds.SetMobStateThreshold(patient, FixedPoint2.Max(deathThreshold!.Value, limit * 2), MobState.Dead);
            var seed = new DamageSpecifier { DamageDict = { ["Blunt"] = limit } };
            seed.Flags = DamageSpecifier.DamageFlags.NoWounds | DamageSpecifier.DamageFlags.NoBleeding;
            _damage.ChangeDamage(patient, seed, ignoreResistances: true);
            _pressure.Update(1f);

            var atLimit = _damage.GetAllDamage(patient).GetTotal();
            Assert.That(atLimit, Is.GreaterThanOrEqualTo(limit));
            Assert.That(_mobState.IsDead(patient), Is.False);
            _pressure.Update(1f);
            Assert.That(_damage.GetAllDamage(patient).GetTotal(), Is.GreaterThan(atLimit),
                "A living patient must continue taking pressure damage beyond the cap.");

            _mobState.ChangeMobState(patient, MobState.Dead);
            Assert.That(SEntMan.Deleted(patient) || SEntMan.IsQueuedForDeletion(patient), Is.False);
            Assert.That(_mobState.IsDead(patient), Is.True);
            var stoppedDamage = _damage.GetAllDamage(patient).GetTotal();
            Assert.That(stoppedDamage, Is.GreaterThanOrEqualTo(limit));
            Assert.That(_body.GetExternalOrgans(patient).Count(), Is.EqualTo(parts.Length));
            Assert.That(parts.All(part => !SEntMan.Deleted(part) && !SEntMan.IsQueuedForDeletion(part)), Is.True);
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
