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

    [TestCase(false)]
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
