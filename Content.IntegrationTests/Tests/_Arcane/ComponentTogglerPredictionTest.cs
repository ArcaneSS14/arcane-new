// Arcane-Start
using Content.IntegrationTests.Tests.Helpers;
using Content.Shared.CombatMode;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class ComponentTogglerPredictionTest : GameTest
{
    private sealed class ToggleListenerSystem : TestListenerSystem<ItemToggledEvent>;

    private const string SelfToggler = "TestComponentTogglerSelf";
    private const string ParentToggler = "TestComponentTogglerParent";

    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  id: TestComponentTogglerSelf
  components:
  - type: ItemToggle
  - type: ComponentToggler
    components:
    - type: DisarmMalus
    deactivateComponents:
    - type: EyeProtection

- type: entity
  parent: TestComponentTogglerSelf
  id: TestComponentTogglerParent
  components:
  - type: ComponentToggler
    parent: true
""";

    [Test]
    public async Task FixtureSpawnsBothTogglers()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            foreach (var prototype in new[] { SelfToggler, ParentToggler })
            {
                var holder = SSpawnAtPosition(null, map.GridCoords);
                var item = SSpawnAtPosition(prototype, new EntityCoordinates(holder, Vector2.Zero));
                Server.System<SharedTransformSystem>().SetParent(item, holder);
                var parent = prototype == ParentToggler;
                var target = parent ? holder : item;
                Assert.That(SEntMan.HasComponent<ComponentTogglerComponent>(item), Is.True);
                Assert.That(SComp<ItemToggleComponent>(item).Activated, Is.False);
                var toggler = SComp<ComponentTogglerComponent>(item);
                Assert.That(toggler.Parent, Is.EqualTo(parent));
                Assert.That(toggler.Components.Keys, Does.Contain("DisarmMalus"));
                Assert.That(toggler.DeactivateComponents.Keys, Does.Contain("EyeProtection"));
                AssertServerActivation(item, holder, target, parent);
                Assert.That(Server.System<ItemToggleSystem>().TryDeactivate(item), Is.True);
                Assert.That(SEntMan.HasComponent<DisarmMalusComponent>(target), Is.False);
                Assert.That(SEntMan.HasComponent<EyeProtectionComponent>(target), Is.True);
            }
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ClientCanPredictDeactivationAfterServerActivation(bool parent)
    {
        var map = await Pair.CreateTestMap();
        NetEntity itemNet = default;
        NetEntity holderNet = default;
        EntityUid item = default;
        EntityUid holder = default;
        EntityUid target = default;
        await Server.WaitAssertion(() =>
        {
            holder = SSpawnAtPosition(null, map.GridCoords);
            item = SSpawnAtPosition(parent ? ParentToggler : SelfToggler,
                new EntityCoordinates(holder, Vector2.Zero));
            Server.System<SharedTransformSystem>().SetParent(item, holder);
            target = parent ? holder : item;
            itemNet = SEntMan.GetNetEntity(item);
            holderNet = SEntMan.GetNetEntity(holder);
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var clientItem = CEntMan.GetEntity(itemNet);
            var clientTarget = parent ? CEntMan.GetEntity(holderNet) : clientItem;
            Assert.That(CComp<TransformComponent>(clientItem).ParentUid, Is.EqualTo(CEntMan.GetEntity(holderNet)));
            Assert.That(CComp<ItemToggleComponent>(clientItem).Activated, Is.False);
            Assert.That(CEntMan.HasComponent<DisarmMalusComponent>(clientTarget), Is.False);
        });

        await Server.WaitAssertion(() =>
        {
            AssertServerActivation(item, holder, target, parent);
        });
        await Pair.RunUntilSynced();

        await Client.WaitAssertion(() =>
        {
            var clientItem = CEntMan.GetEntity(itemNet);
            var clientTarget = parent ? CEntMan.GetEntity(holderNet) : clientItem;
            Assert.That(CComp<ItemToggleComponent>(clientItem).Activated, Is.True);
            Assert.That(CEntMan.HasComponent<DisarmMalusComponent>(clientTarget), Is.True);
            var clientToggler = CComp<ComponentTogglerComponent>(clientItem);
            Assert.That(clientToggler.Parent, Is.EqualTo(parent));
            Assert.That(clientToggler.Target, Is.Null);

            Assert.That(Client.System<ItemToggleSystem>().TryDeactivate(clientItem), Is.True);

            Assert.That(CEntMan.HasComponent<DisarmMalusComponent>(clientTarget), Is.False,
                "A client that only received the active state must remove the active components immediately.");
            Assert.That(CEntMan.HasComponent<EyeProtectionComponent>(clientTarget), Is.True,
                "The target's deactivated components must also be restored during prediction.");
        });
        await Server.WaitAssertion(() =>
        {
            Assert.That(SComp<ItemToggleComponent>(item).Activated, Is.True);
            Assert.That(SEntMan.HasComponent<DisarmMalusComponent>(target), Is.True);
        });
    }

    private void AssertServerActivation(EntityUid item, EntityUid holder, EntityUid target, bool parent)
    {
        Assert.That(SEntMan, Is.SameAs(Server.EntMan));
        SEntMan.EnsureComponent<TestListenerComponent>(item);
        SEntMan.EnsureComponent<TestListenerComponent>(holder);
        var listener = Server.System<ToggleListenerSystem>();
        listener.Clear(item);
        listener.Clear(holder);
        var toggler = SComp<ComponentTogglerComponent>(item);
        var toggle = SComp<ItemToggleComponent>(item);
        TestContext.Out.WriteLine($"Before TryActivate item={item} itemNet={SEntMan.GetNetEntity(item)} holder={holder} holderNet={SEntMan.GetNetEntity(holder)} owner={toggler.Owner} transformParent={SComp<TransformComponent>(item).ParentUid} Parent={toggler.Parent} Target={toggler.Target} Activated={toggle.Activated}");
        Assert.That(toggler.Owner, Is.EqualTo(item));
        Assert.That(SComp<TransformComponent>(item).ParentUid, Is.EqualTo(holder));
        Assert.That(toggler.Parent, Is.EqualTo(parent));
        Assert.That(toggler.Components.Keys, Does.Contain("DisarmMalus"));
        Assert.That(toggler.Target, Is.Null);
        Assert.That(toggle.Activated, Is.False);
        var activated = Server.System<ItemToggleSystem>().TryActivate(item);
        TestContext.Out.WriteLine($"After TryActivate result={activated} item={item} owner={toggler.Owner} Parent={toggler.Parent} Target={toggler.Target} Activated={toggle.Activated} itemEvents={listener.Count(item)} holderEvents={listener.Count(holder)}");
        Assert.That(activated, Is.True);
        Assert.That(toggle.Owner, Is.EqualTo(item));
        Assert.That(toggle.Activated, Is.True);
        Assert.That(listener.Count(item, ev => ev.Activated), Is.EqualTo(1));
        Assert.That(listener.Count(holder), Is.Zero);
        Assert.That(toggler.Target, Is.EqualTo(target));
        Assert.That(SEntMan.HasComponent<DisarmMalusComponent>(target), Is.True);
    }
}
// Arcane-End
