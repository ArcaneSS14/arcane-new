using System.Linq;
using Content.Arcane.Server.Medical.Respiration;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;

namespace Content.IntegrationTests.Tests._Arcane.Medical;

[TestOf(typeof(CPRRecoverySystem))]
public sealed class CPRRecoveryTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly DamageableSystem _damage = null!;
    [SidedDependency(Side.Server)] private readonly MobStateSystem _mob = null!;

    [TestPrototypes]
    private const string CPRRescuer = """
- type: entity
  parent: MobHuman
  id: TestArcaneCPRRescuer
  components:
  - type: CPRTraining
    duration: 0.1
    reviveChance: 1
    inhaleChance: 0
""";

    [Test]
    public async Task CompletedCPRRetainsTheOriginalRevivalChance()
    {
        var patient = await CreatePatient();
        await Server.WaitPost(() =>
        {
            var rescuer = SSpawnAtPosition("TestArcaneCPRRescuer", TestMap!.GridCoords);
            _damage.ChangeDamage(patient, new() { DamageDict = { ["Asphyxiation"] = 180 } },
                targetPart: Content.Medical.Common.Targeting.TargetBodyPart.Vital, ignoreResistances: true);
            _mob.ChangeMobState(patient, MobState.Dead);
            var cpr = SEntMan.System<SharedVerbSystem>().GetLocalVerbs(patient, rescuer, typeof(InnateVerb))
                .Single(verb => verb.Text == Loc.GetString("cpr-verb"));
            cpr.Act!.Invoke();
        });
        await RunSeconds(0.25f);
        await Server.WaitPost(() =>
        {
            Assert.That(_mob.IsDead(patient), Is.False, "CPR must retain its existing revival chance.");
            Assert.That(GetAsphyxiation(patient), Is.LessThan(180));
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
