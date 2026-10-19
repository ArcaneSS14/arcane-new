#nullable enable
using System.Linq;
using Content.Arcane.Client.Medical.HealthAnalyzer;
using Content.Arcane.Common.Medical.HealthAnalyzer;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane.Medical;

[TestOf(typeof(MedicalDiagnosticsSystem))]
public sealed class WoundDiagnosticsTest : GameTest
{
    private static readonly EntProtoId BluntWound = "Blunt";
    private static readonly ProtoId<DamageContainerPrototype> Silicon = "Silicon";

    [SidedDependency(Side.Server)] private readonly BodySystem _body = null!;
    [SidedDependency(Side.Server)] private readonly WoundSystem _wounds = null!;
    [SidedDependency(Side.Server)] private readonly DamageableSystem _damage = null!;

    [Test]
    [RunOnSide(Side.Server)]
    public void WoundsUseDamageUnitsAndCanHealAfterRawDamageIsGone()
    {
        foreach (var type in new[] { "Blunt", "Bloodloss", "Asphyxiation" })
        {
            var patient = SSpawn("MobHuman");
            var torso = _body.GetOrgan(patient, "Torso")!.Value;
            var part = new Entity<WoundableComponent?>(torso, SComp<WoundableComponent>(torso));
            var damage = new DamageSpecifier { DamageDict = { [type] = 10 } };
            damage.WoundSeverityMultipliers[type] = 4;
            _damage.TryChangeDamage(torso, damage, ignoreResistances: true);
            var wound = _wounds.GetWoundableWounds(part).Single(w => w.Comp.DamageType == type);
            Assert.That(wound.Comp.WoundSeverityPoint.Int(), Is.EqualTo(10), type);

            // A blocked wound can outlive the corresponding raw damage.
            wound.Comp.CanBeHealed = false;
            var healing = new DamageSpecifier { DamageDict = { [type] = -10 } };
            _damage.TryChangeDamage(torso, healing, ignoreResistances: true);
            Assert.That(_damage.GetTotalDamage(torso).Int(), Is.Zero, type);
            Assert.That(wound.Comp.WoundSeverityPoint.Int(), Is.EqualTo(10), type);

            _damage.TryChangeDamage(torso, healing, ignoreResistances: true, ignoreBlockers: true);
            Assert.That(_wounds.GetWoundableWounds(part).Any(w => w.Comp.DamageType == type), Is.False, type);
        }
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void RejectedDamageTypesDoNotCreateWounds()
    {
        var arm = SSpawn("OrganCyberneticArmLeft");
        SComp<InjurableComponent>(arm).DamageContainer = Silicon;
        _damage.TryChangeDamage(arm,
            new DamageSpecifier { DamageDict = { ["Asphyxiation"] = 10, ["Blunt"] = 10 } },
            ignoreResistances: true);
        var wounds = _wounds.GetWoundableWounds((arm, SComp<WoundableComponent>(arm)));
        Assert.That(wounds.Any(w => w.Comp.DamageType == "Asphyxiation"), Is.False);
        Assert.That(wounds.Single(w => w.Comp.DamageType == "Blunt").Comp.WoundSeverityPoint.Int(), Is.EqualTo(10));
    }

    [Test]
    public async Task AnalyzerUsesWoundsForTheWholeBodyAndSelectedPart()
    {
        await Pair.CreateTestMap();
        NetEntity patientNet = default;
        NetEntity torsoNet = default;
        EntityUid? previousAttachment = null;
        try
        {
            await Server.WaitAssertion(() =>
            {
                previousAttachment = ServerSession!.AttachedEntity;
                var patient = SSpawnAtPosition("MobHuman", TestMap!.GridCoords);
                var torso = _body.GetOrgan(patient, "Torso")!.Value;
                var arm = _body.GetOrgan(patient, "ArmLeft")!.Value;
                Assert.That(_wounds.TryInduceWound((torso, SComp<WoundableComponent>(torso)), BluntWound, 10, out _), Is.True);
                Assert.That(_wounds.TryInduceWound((arm, SComp<WoundableComponent>(arm)), BluntWound, 20, out _), Is.True);
                Assert.That(_damage.GetTotalDamage(patient).Int(), Is.Zero);
                patientNet = SEntMan.GetNetEntity(patient);
                torsoNet = SEntMan.GetNetEntity(torso);
                Server.PlayerMan.SetAttachedEntity(ServerSession, patient);
            });
            await Pair.RunUntilSynced();
            await Client.WaitAssertion(() =>
            {
                var patient = CEntMan.GetEntity(patientNet);
                var whole = new HealthAnalyzerPopulateEvent();
                CEntMan.EventBus.RaiseLocalEvent(patient, ref whole);
                Assert.That(whole.DiagnosticDamage!.GetTotal().Int(), Is.EqualTo(30));
                Assert.That(whole.Pulse, Is.Not.Null);

                var selected = new HealthAnalyzerPopulateEvent(CEntMan.GetEntity(torsoNet));
                CEntMan.EventBus.RaiseLocalEvent(patient, ref selected);
                Assert.That(selected.DiagnosticDamage!.GetTotal().Int(), Is.EqualTo(10));
                Assert.That(selected.Pulse, Is.EqualTo(whole.Pulse));
            });

            NetEntity headNet = default;
            await Server.WaitAssertion(() =>
            {
                var patient = SEntMan.GetEntity(patientNet);
                var head = _body.GetOrgan(patient, "Head")!.Value;
                var arm = _body.GetOrgan(patient, "ArmLeft")!.Value;
                _damage.TryChangeDamage(head, new DamageSpecifier { DamageDict = { ["Blunt"] = 20 } }, ignoreResistances: true);
                Assert.That(_wounds.TryHealWounds((head, SComp<WoundableComponent>(head)), 20, out _), Is.True);
                _damage.TryChangeDamage(arm, new DamageSpecifier { DamageDict = { ["Blunt"] = 5 } }, ignoreResistances: true);
                headNet = SEntMan.GetNetEntity(head);
            });
            await Pair.RunUntilSynced();
            await Client.WaitAssertion(() =>
            {
                var patient = CEntMan.GetEntity(patientNet);
                var selected = new HealthAnalyzerPopulateEvent(CEntMan.GetEntity(headNet));
                CEntMan.EventBus.RaiseLocalEvent(patient, ref selected);
                Assert.That(selected.DiagnosticDamage!.GetTotal().Int(), Is.EqualTo(20), "Raw damage remains visible after wounds heal.");

                var whole = new HealthAnalyzerPopulateEvent();
                CEntMan.EventBus.RaiseLocalEvent(patient, ref whole);
                Assert.That(whole.DiagnosticDamage!.GetTotal().Int(), Is.EqualTo(55),
                    "Include raw damage on one part and wounds on others without counting the same injury twice.");
            });
        }
        finally
        {
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, previousAttachment));
        }
    }
}
