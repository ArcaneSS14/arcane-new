// Arcane-Start
using Content.Arcane.Common.Preferences;
using Content.Arcane.Shared.ERP;
using Content.Arcane.Shared.ErpPanel;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class ErpPanelAuthorizationTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    private const string Interaction = "TestPanelAuthorization";

    [TestPrototypes]
    private const string Prototypes = """
- type: panelInteractionCategory
  id: TestPanelAuthorization

- type: panelInteraction
  id: TestPanelAuthorization
  category: TestPanelAuthorization
  icon: /Textures/Interface/VerbIcons/bubbles.svg.192dpi.png
  tags:
  - RequiresERP
  messages:
  - Test interaction.
  selfMessages:
  - Test interaction.
""";

    public override async Task DoTeardown()
    {
        await Server.WaitPost(() =>
        {
            if (SEntMan.EntityExists(SPlayer))
                SEntMan.System<SharedUserInterfaceSystem>().CloseUi(SPlayer, ErpPanelKey.Key, SPlayer);
        });
        await base.DoTeardown();
    }

    [TestCase(ErpPreference.No, ErpPreference.Yes, false)]
    [TestCase(ErpPreference.Yes, ErpPreference.No, false)]
    [TestCase(ErpPreference.No, ErpPreference.No, false)]
    [TestCase(ErpPreference.Yes, ErpPreference.Yes, true)]
    [TestCase(ErpPreference.Ask, ErpPreference.Yes, true)]
    [TestCase(ErpPreference.Yes, ErpPreference.Ask, true)]
    public async Task SubmitUsesBothCurrentPreferences(ErpPreference userPreference, ErpPreference targetPreference, bool accepted)
    {
        await Server.WaitAssertion(() =>
        {
            var target = OpenPanel();
            SEntMan.GetComponent<ErpStatusComponent>(SPlayer).Preference = userPreference;
            SEntMan.GetComponent<ErpStatusComponent>(target).Preference = targetPreference;

            Send(SPlayer);

            Assert.That(SEntMan.GetComponent<ErpPanelOwnerComponent>(SPlayer).Cooldowns.ContainsKey(Interaction), Is.EqualTo(accepted));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task SubmitRechecksBothAges(bool initiator)
    {
        await Server.WaitAssertion(() =>
        {
            var target = OpenPanel();
            SEntMan.GetComponent<HumanoidProfileComponent>(initiator ? SPlayer : target).Age = HumanoidCharacterProfile.ErpMinAge - 1;

            Send(SPlayer);

            Assert.That(SEntMan.GetComponent<ErpPanelOwnerComponent>(SPlayer).Cooldowns, Is.Empty);
        });
    }

    [Test]
    public async Task SubmitRejectsAnotherActorAndTargetMovingOutOfRange()
    {
        await Server.WaitAssertion(() =>
        {
            var target = OpenPanel();
            Send(target);
            Assert.That(SEntMan.GetComponent<ErpPanelOwnerComponent>(SPlayer).Cooldowns, Is.Empty);

            SEntMan.System<SharedTransformSystem>().SetCoordinates(target, MapData.GridCoords.Offset(new Vector2(20, 20)));
            Send(SPlayer);
            Assert.That(SEntMan.GetComponent<ErpPanelOwnerComponent>(SPlayer).Cooldowns, Is.Empty);
        });
    }

    [Test]
    public async Task SubmitUsesTheCurrentServerTarget()
    {
        await Server.WaitAssertion(() =>
        {
            var oldTarget = OpenPanel();
            SEntMan.GetComponent<ErpStatusComponent>(oldTarget).Preference = ErpPreference.No;
            var currentTarget = OpenPanel();

            Send(SPlayer);

            var panel = SEntMan.GetComponent<ErpPanelOwnerComponent>(SPlayer);
            Assert.That(panel.Target, Is.EqualTo(currentTarget));
            Assert.That(panel.Cooldowns.ContainsKey(Interaction), Is.True);
        });
    }

    [Test]
    public async Task ClosingThePanelRejectsAQueuedSubmit()
    {
        await Server.WaitAssertion(() =>
        {
            OpenPanel();
            SEntMan.System<SharedUserInterfaceSystem>().CloseUi(SPlayer, ErpPanelKey.Key, SPlayer);
            Assert.That(SEntMan.GetComponent<ErpPanelOwnerComponent>(SPlayer).Target, Is.Null);

            Send(SPlayer);

            Assert.That(SEntMan.GetComponent<ErpPanelOwnerComponent>(SPlayer).Cooldowns, Is.Empty);
        });
    }

    [Test]
    public async Task AnotherActorClosingDoesNotClearOwnersTarget()
    {
        await Server.WaitAssertion(() =>
        {
            var other = OpenPanel();
            var close = new BoundUIClosedEvent(ErpPanelKey.Key, SPlayer, other);
            SEntMan.EventBus.RaiseLocalEvent(SPlayer, close);
            Assert.That(SEntMan.GetComponent<ErpPanelOwnerComponent>(SPlayer).Target, Is.EqualTo(other));
            SEntMan.System<SharedUserInterfaceSystem>().CloseUi(SPlayer, ErpPanelKey.Key, SPlayer);
            Assert.That(SEntMan.GetComponent<ErpPanelOwnerComponent>(SPlayer).Target, Is.Null);
        });
    }

    private EntityUid OpenPanel()
    {
        var target = SEntMan.SpawnAttachedTo("MobHuman", MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f)));
        foreach (var actor in new[] { SPlayer, target })
        {
            SEntMan.GetComponent<HumanoidProfileComponent>(actor).Age = 30;
            SEntMan.EnsureComponent<ErpPanelOwnerComponent>(actor);
            SEntMan.EnsureComponent<ArousalComponent>(actor);
            SEntMan.EnsureComponent<ErpStatusComponent>(actor).Preference = ErpPreference.Yes;
        }

        var open = new ErpPanelOpenEvent(target);
        SEntMan.EventBus.RaiseLocalEvent(SPlayer, ref open);
        Assert.That(SEntMan.GetComponent<ErpPanelOwnerComponent>(SPlayer).Target, Is.EqualTo(target));
        return target;
    }

    private void Send(EntityUid actor)
    {
        SEntMan.EventBus.RaiseLocalEvent(SPlayer, new ErpPanelSendMessage(Interaction)
        {
            Actor = actor,
            UiKey = ErpPanelKey.Key,
        });
    }
}
// Arcane-End
