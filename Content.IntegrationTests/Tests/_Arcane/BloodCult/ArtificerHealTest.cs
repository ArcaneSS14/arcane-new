#nullable enable
using Content.IntegrationTests.Fixtures;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Trauma.Common.Weapons;
using Content.Trauma.Shared.BloodCult;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane.BloodCult;

public sealed class ArtificerHealTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = false };
    private static readonly EntProtoId Artificer = "ConstructArtificer";
    private static readonly EntProtoId Construct = "ConstructJuggernaut";
    private static readonly EntProtoId CultRule = "BloodCult";

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public async Task HealingRequiresMembershipInTheSameCult(int targetCult)
    {
        await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var coords = TestMap!.GridCoords;
            var user = SSpawnAtPosition(Artificer, coords);
            var target = SSpawnAtPosition(Construct, coords);
            var rule = SSpawnAtPosition(CultRule, coords);
            SEnsureComp<BloodCultMemberComponent>(user).Rule = rule;
            if (targetCult != 0)
                SEnsureComp<BloodCultMemberComponent>(target).Rule = targetCult == 1
                    ? rule : SSpawnAtPosition(CultRule, coords);

            var damage = SEntMan.System<DamageableSystem>();
            damage.ChangeDamage(target, new DamageSpecifier { DamageDict = { ["Blunt"] = 10 } }, ignoreResistances: true);
            var before = damage.GetTotalDamage(target);
            Assert.That(before.Int(), Is.GreaterThan(0));

            var ev = new BeforeHarmfulActionEvent(user, target, HarmfulActionType.Harm);
            SEntMan.EventBus.RaiseLocalEvent(target, ref ev);
            Assert.That(ev.Cancelled, Is.EqualTo(targetCult == 1));
            Assert.That(damage.GetTotalDamage(target), targetCult == 1 ? Is.LessThan(before) : Is.EqualTo(before));
        });
    }
}
