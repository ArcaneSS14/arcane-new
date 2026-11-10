#nullable enable
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Wagging;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class WaggingAvailabilityTest : InteractionTest
{
    private static readonly ProtoId<OrganCategoryPrototype> Tail = "Tail";
    private static readonly ProtoId<MarkingPrototype> Still = "LizardTailSmooth";
    private static readonly ProtoId<MarkingPrototype> Animated = "LizardTailSmoothAnimated";
    // These IDs belong to TestPrototypes, not the production YAML validated by static-field checks.
    private const string ExplicitStill = "ArcaneWaggingStillTest";
    private const string ExplicitAnimated = "ArcaneWaggingMotionTest";
    protected override string PlayerPrototype => "MobReptilian";

    [Test]
    public async Task MarkingChangesAndTailRemovalUpdateTheActionWithoutOrphans()
    {
        EntityUid oldAction = default;
        EntityUid tail = default;
        await Server.WaitPost(() => SetTail(false));
        await Pair.RunTicksSync(3);
        await Server.WaitAssertion(() =>
        {
            var wagging = SEntMan.GetComponent<WaggingComponent>(SPlayer);
            Assert.That(wagging.ActionEntity, Is.Null);
            var markingManager = Server.ResolveDependency<MarkingManager>();
            Assert.That(markingManager.TryGetWaggingMarking(Still, out var mapped), Is.True);
            Assert.That(mapped, Is.EqualTo(Animated.Id));
            SetTail(true);
        });
        await Pair.RunUntilSynced();
        await Server.WaitAssertion(() =>
        {
            var wagging = SEntMan.GetComponent<WaggingComponent>(SPlayer);
            Assert.That(wagging.ActionEntity, Is.Not.Null);
            oldAction = wagging.ActionEntity!.Value;
            Assert.That(SEntMan.System<WaggingSystem>().TryToggleWagging((SPlayer, wagging), true), Is.True);
            var organ = SEntMan.System<BodySystem>().GetOrgan(SPlayer, Tail)!.Value;
            var marking = SEntMan.GetComponent<VisualOrganMarkingsComponent>(organ).Markings[HumanoidVisualLayers.Tail][0];
            Assert.That(marking.MarkingId, Is.EqualTo(Animated));
            Assert.That(marking.MarkingColors[0], Is.EqualTo(Color.Red));
            Assert.That(marking.MarkingColors[1], Is.EqualTo(Color.Blue));
            SetTail(false);
        });
        await Pair.RunUntilSynced();
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.GetComponent<WaggingComponent>(SPlayer).ActionEntity, Is.Null);
            Assert.That(SEntMan.EntityExists(oldAction), Is.False);
            SetTail(true);
        });
        await Pair.RunUntilSynced();
        await Server.WaitAssertion(() =>
        {
            var bodySystem = SEntMan.System<BodySystem>();
            var action = SEntMan.GetComponent<WaggingComponent>(SPlayer).ActionEntity;
            Assert.That(action, Is.Not.Null.And.Not.EqualTo(oldAction));
            oldAction = action!.Value;
            tail = bodySystem.GetOrgan(SPlayer, Tail)!.Value;
            Assert.That(bodySystem.RemoveOrgan((SPlayer, SEntMan.GetComponent<BodyComponent>(SPlayer)),
                (tail, SEntMan.GetComponent<OrganComponent>(tail))), Is.True);
        });
        await Pair.RunUntilSynced();
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<WaggingComponent>(SPlayer), Is.False);
            Assert.That(SEntMan.EntityExists(oldAction), Is.False);
            Assert.That(SEntMan.System<BodySystem>().InsertOrgan((SPlayer, SEntMan.GetComponent<BodyComponent>(SPlayer)),
                (tail, SEntMan.GetComponent<OrganComponent>(tail))), Is.True);
        });
        await Pair.RunUntilSynced();
        await Server.WaitAssertion(() => Assert.That(SEntMan.GetComponent<WaggingComponent>(SPlayer).ActionEntity, Is.Not.Null));
    }

    [Test]
    public async Task ExplicitWaggingReferenceWorksWithoutTheLegacySuffix()
    {
        await Server.WaitPost(() => SetTail(true, ExplicitStill));
        await Pair.RunUntilSynced();
        await Server.WaitAssertion(() =>
        {
            var manager = Server.ResolveDependency<MarkingManager>();
            Assert.That(manager.TryGetWaggingMarking(ExplicitStill, out var mapped), Is.True);
            Assert.That(mapped, Is.EqualTo(ExplicitAnimated));
            var component = SEntMan.GetComponent<WaggingComponent>(SPlayer);
            Assert.That(component.ActionEntity, Is.Not.Null);
            var wagging = SEntMan.System<WaggingSystem>();
            Assert.That(wagging.TryToggleWagging((SPlayer, component), true), Is.True);
            var organ = SEntMan.System<BodySystem>().GetOrgan(SPlayer, Tail)!.Value;
            var marking = SEntMan.GetComponent<VisualOrganMarkingsComponent>(organ).Markings[HumanoidVisualLayers.Tail][0];
            Assert.That(marking.MarkingId.Id, Is.EqualTo(ExplicitAnimated));
            Assert.That(marking.MarkingColors, Is.EqualTo(new[] {Color.Red, Color.Blue}));
            Assert.That(wagging.TryToggleWagging((SPlayer, component), false), Is.True);
            marking = SEntMan.GetComponent<VisualOrganMarkingsComponent>(organ).Markings[HumanoidVisualLayers.Tail][0];
            Assert.That(marking.MarkingId.Id, Is.EqualTo(ExplicitStill));
        });
    }

    private void SetTail(bool present, ProtoId<MarkingPrototype>? marking = null)
    {
        SEntMan.System<SharedVisualBodySystem>().ApplyMarkings(SPlayer, new()
        {
            [Tail] = new() { [HumanoidVisualLayers.Tail] = present ? [new Marking(marking ?? Still, [Color.Red, Color.Blue])] : [] },
        });
    }

    [TestPrototypes]
    private const string Prototypes = """
- type: marking
  id: ArcaneWaggingStillTest
  bodyPart: Tail
  groupWhitelist: []
  wagging: ArcaneWaggingMotionTest
  sprites:
  - sprite: Mobs/Customization/reptilian_parts.rsi
    state: tail_smooth_primary
  - sprite: Mobs/Customization/reptilian_parts.rsi
    state: tail_smooth_secondary

- type: marking
  id: ArcaneWaggingMotionTest
  bodyPart: Tail
  groupWhitelist: []
  sprites:
  - sprite: Mobs/Customization/reptilian_parts.rsi
    state: tail_smooth_wagging_primary
  - sprite: Mobs/Customization/reptilian_parts.rsi
    state: tail_smooth_wagging_secondary
""";
}
