#nullable enable
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Tests.Interaction;
using Content.Medical.Common.Targeting;
using Content.Medical.Shared.Wounds;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Medical.Healing;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane.Medical;

[TestOf(typeof(HealingSystem))]
public sealed class WoundHealingBudgetTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";
    private static readonly EntProtoId BluntWound = "Blunt";
    [SidedDependency(Side.Server)] private readonly BodySystem _body = null!;
    [SidedDependency(Side.Server)] private readonly WoundSystem _wounds = null!;
    [SidedDependency(Side.Server)] private readonly DamageableSystem _damage = null!;

    [TestCase(false)]
    [TestCase(true)]
    public async Task OnePackCannotSpendTheSameHealingOnTwoLimbs(bool mixedDamage)
    {
        await SpawnTarget("MobHuman");
        var pack = await PlaceInHands("Brutepack", 1);
        await Server.WaitPost(() =>
        {
            var patient = STarget!.Value;
            var selection = new GetTargetedPartEvent(patient);
            SEntMan.EventBus.RaiseLocalEvent(SPlayer, ref selection);
            Assert.That(selection.Part, Is.Not.Null);
            var first = selection.Part!.Value;
            var second = _body.GetOrgan(patient, "ArmLeft")!.Value;
            if (second == first)
                second = _body.GetOrgan(patient, "Torso")!.Value;
            if (mixedDamage)
                _damage.TryChangeDamage(first, new DamageSpecifier { DamageDict = { ["Blunt"] = 5 } }, ignoreResistances: true);
            Assert.That(_wounds.TryInduceWound((first, SComp<WoundableComponent>(first)), BluntWound,
                mixedDamage ? 5 : 10, out _), Is.True);
            Assert.That(_wounds.TryInduceWound((second, SComp<WoundableComponent>(second)), BluntWound, 10, out _), Is.True);
            var uid = SEntMan.GetEntity(pack);
            var healing = SComp<HealingComponent>(uid);
            healing.Damage = new DamageSpecifier { DamageDict = { ["Blunt"] = -10 } };
            healing.Delay = TimeSpan.FromSeconds(0.1);
            SEntMan.Dirty(uid, healing);
        });
        await RunSeconds(0.2f);
        await Interact();
        await RunSeconds(0.4f);
        await Server.WaitPost(() =>
        {
            Assert.That(_wounds.TryGetBodyWounds(STarget!.Value, out var wounds), Is.True);
            Assert.That(wounds.Where(w => w.Comp.DamageType == "Blunt").Sum(w => w.Comp.WoundSeverityPoint.Int()), Is.EqualTo(10));
        });
    }
}
