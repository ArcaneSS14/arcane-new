#nullable enable
using System.Linq;
using Content.Arcane.Shared.LimbGnawing;
using Content.IntegrationTests.Tests.Interaction;
using Content.Medical.Common.Body;
using Content.Medical.Shared.Body;
using Content.Server.NPC.HTN;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Verbs;
using Content.Trauma.Common.CCVar;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class LimbGnawingPortTest : InteractionTest
{
    private static readonly ProtoId<SatiationTypePrototype> Hunger = "Hunger";
    protected override string PlayerPrototype => "TestArcaneGnawMouse";

    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  parent: MobMouse
  id: TestArcaneGnawMouse
  components:
  - type: Respirator
    updateInterval: 1000
  - type: Godmode
- type: entity
  parent: MobHuman
  id: TestArcaneGnawHuman
  components:
  - type: Respirator
    updateInterval: 1000
""";

    [Test]
    public async Task HungryNpcChoosesGnawVerbThroughItsHtn()
    {
        await OverrideCVar(Side.Server, TraumaCVars.DisablePathfinding, false);
        await SetTile(Plating, TargetCoords);
        var corpse = ToServer(await Spawn("TestArcaneGnawHuman"));
        EntityUid mouse = default;
        var before = 0;
        var after = 0;
        var sawGnaw = false;
        await Server.WaitAssertion(() =>
        {
            SEntMan.RemoveComponent<HTNComponent>(SPlayer);
            mouse = SEntMan.SpawnAtPosition("TestArcaneGnawMouse", SEntMan.GetCoordinates(PlayerCoords));
            var nutrition = SEntMan.System<SatiationSystem>();
            var satiation = SEntMan.GetComponent<SatiationComponent>(mouse);
            Assert.That(nutrition.GetValueOrNull((mouse, satiation), Hunger), Is.Not.Null);
            nutrition.SetValue((mouse, satiation), Hunger, 0f);
            SEntMan.System<MobStateSystem>().ChangeMobState(corpse, MobState.Dead);
            before = Parts(corpse).Length;
        });
        for (var i = 0; i < 80; i++)
        {
            await RunSeconds(0.25f);
            await Server.WaitPost(() =>
            {
                sawGnaw |= SEntMan.GetComponent<DoAfterComponent>(mouse).DoAfters.Values
                    .Any(value => value.Args.Event is LimbGnawDoAfterEvent && !value.Cancelled);
                after = Parts(corpse).Length;
            });
            if (after < before)
                break;
        }
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<MobStateSystem>().IsAlive(mouse), Is.True);
            Assert.That(sawGnaw, Is.True, "The actual NPC alternative interaction must select the gnaw do-after.");
            Assert.That(after, Is.LessThan(before));
        });
    }

    [Test]
    public async Task GnawDetachesARealPartAndRevivalPreventsTheNextRemoval()
    {
        var corpse = ToServer(await Spawn("TestArcaneGnawHuman"));
        EntityUid[] before = [];
        await Server.WaitAssertion(() =>
        {
            SEntMan.RemoveComponent<HTNComponent>(SPlayer);
            Assert.That(SEntMan.HasComponent<GnawableCorpseComponent>(corpse), Is.False);
            SEntMan.System<MobStateSystem>().ChangeMobState(corpse, MobState.Dead);
            Assert.That(SEntMan.HasComponent<GnawableCorpseComponent>(corpse), Is.True);
            before = Parts(corpse);
            Assert.That(before, Is.Not.Empty);
            StartGnaw(SPlayer, corpse);
        });
        await RunSeconds(6.2f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<MobStateSystem>().IsAlive(SPlayer), Is.True);
            var after = Parts(corpse);
            Assert.That(after, Has.Length.EqualTo(before.Length - 1));
            var detached = before.Except(after).Single();
            Assert.That(SEntMan.EntityExists(detached), Is.True);
            Assert.That(SEntMan.System<BodySystem>().GetBody(detached), Is.Null);
            StartGnaw(SPlayer, corpse);
        });
        await RunSeconds(5.9f);
        await Server.WaitAssertion(() =>
        {
            SEntMan.System<MobStateSystem>().ChangeMobState(corpse, MobState.Alive);
            Assert.That(SEntMan.HasComponent<GnawableCorpseComponent>(corpse), Is.False);
        });
        await RunSeconds(0.4f);
        await Server.WaitAssertion(() => Assert.That(Parts(corpse), Has.Length.EqualTo(before.Length - 1)));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CompetingGnawersCannotDuplicateTheLastPartAndRemovedAbilitiesCannotAct(bool removeAbilities)
    {
        var corpse = ToServer(await Spawn("TestArcaneGnawHuman"));
        EntityUid last = default;
        await Server.WaitAssertion(() =>
        {
            var body = SEntMan.System<BodySystem>();
            var parts = Parts(corpse);
            last = parts[0];
            foreach (var part in parts.Skip(1))
                Assert.That(body.RemoveOrgan(corpse, part), Is.True);
            SEntMan.System<MobStateSystem>().ChangeMobState(corpse, MobState.Dead);
            Assert.That(Parts(corpse), Has.Length.EqualTo(1));

            var first = SEntMan.SpawnAtPosition("TestArcaneGnawMouse", SEntMan.GetCoordinates(PlayerCoords));
            var second = SEntMan.SpawnAtPosition("TestArcaneGnawMouse", SEntMan.GetCoordinates(PlayerCoords));
            SEntMan.RemoveComponent<HTNComponent>(first);
            SEntMan.RemoveComponent<HTNComponent>(second);
            StartGnaw(first, corpse);
            StartGnaw(second, corpse);
            if (removeAbilities)
            {
                SEntMan.RemoveComponent<LimbGnawerComponent>(first);
                SEntMan.RemoveComponent<LimbGnawerComponent>(second);
            }
        });
        await RunSeconds(6.2f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Parts(corpse), Has.Length.EqualTo(removeAbilities ? 1 : 0));
            Assert.That(SEntMan.EntityExists(last), Is.True);
            Assert.That(SEntMan.System<BodySystem>().GetBody(last), Is.EqualTo(removeAbilities ? corpse : (EntityUid?) null));
            Assert.That(SEntMan.HasComponent<GnawableCorpseComponent>(corpse), Is.EqualTo(removeAbilities));
        });
    }

    private EntityUid[] Parts(EntityUid body)
        => SEntMan.System<BodyPartSystem>().GetBodyParts(body)
            .Where(part => part.Comp.PartType is BodyPartType.Hand or BodyPartType.Foot or BodyPartType.Arm or BodyPartType.Leg)
            .Select(part => part.Owner).ToArray();

    private void StartGnaw(EntityUid user, EntityUid corpse)
    {
        var verbs = SEntMan.System<SharedVerbSystem>().GetLocalVerbs(corpse, user, typeof(AlternativeVerb), force: true);
        var verb = verbs.SingleOrDefault(value => value.Text == Loc.GetString("limb-gnaw-verb"));
        Assert.That(verb, Is.Not.Null,
            $"alive={SEntMan.System<MobStateSystem>().IsAlive(user)} dead={SEntMan.System<MobStateSystem>().IsDead(corpse)} " +
            $"gnawer={SEntMan.HasComponent<LimbGnawerComponent>(user)} tracked={SEntMan.HasComponent<LimbGnawingTargetComponent>(corpse)} " +
            $"marker={SEntMan.HasComponent<GnawableCorpseComponent>(corpse)} parts={Parts(corpse).Length} " +
            $"linked={Parts(corpse).Count(part => SEntMan.System<BodySystem>().GetBody(part) == corpse)} " +
            $"verbs={string.Join(',', verbs.Select(value => value.Text))}");
        Assert.That(verb!.Act, Is.Not.Null);
        verb.Act!();
        var active = SEntMan.GetComponent<DoAfterComponent>(user).DoAfters.Values
            .SingleOrDefault(value => value.Args.Event is LimbGnawDoAfterEvent && !value.Cancelled && !value.Completed);
        Assert.That(active, Is.Not.Null, "The gnaw verb must start its real do-after.");
    }
}
