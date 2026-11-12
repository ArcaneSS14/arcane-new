// Arcane-Start
#nullable enable
using System.Linq;
using Content.Arcane.Server.Cards;
using Content.Arcane.Shared.Cards;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Projectiles;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Random;
using Robust.Shared.Audio.Components;
using Robust.Shared.Player;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class PlayingCardsRuntimeTest : InteractionTest
{
    public override PoolSettings PoolSettings => new() { Connected = true, Dirty = true };

    [TestCase("CardBoxBlack", "CardDeckBlack")]
    [TestCase("CardBoxNanotrasen", "CardDeckNanotrasen")]
    [TestCase("CardBoxSyndicate", "CardDeckSyndicate")]
    public async Task BoxContainsOneMatchingCompleteDeck(string box, string deck)
    {
        await SpawnTarget(box);
        await Server.WaitAssertion(() =>
        {
            var container = SEntMan.System<SharedContainerSystem>().GetContainer(STarget!.Value, StorageComponent.ContainerId);
            Assert.That(container.ContainedEntities.Count, Is.EqualTo(1));
            var entity = container.ContainedEntities.Single();
            Assert.That(SEntMan.GetComponent<MetaDataComponent>(entity).EntityPrototype!.ID, Is.EqualTo(deck));
            Assert.That(Storage(entity).Cards.Count, Is.EqualTo(53));
            Assert.That(Storage(entity).Cards.Select(c => c.Face.Id).Distinct().Count(), Is.EqualTo(53));
        });
    }

    [Test]
    public async Task DealFlipShuffleSplitAndJoinConserveEveryCard()
    {
        await SpawnTarget("CardDeckBlack");
        var deck = STarget!.Value;
        string[] original = [];
        await Server.WaitAssertion(() =>
        {
            original = AllFaces();
            Assert.That(original.Length, Is.EqualTo(53));
        });
        await Activate();
        await RunSeconds(0.6f); // The real deck has a 0.5 second use delay.
        await Activate();
        await Server.WaitAssertion(() =>
        {
            var hand = SEntMan.System<SharedHandsSystem>().GetActiveItem(SPlayer)!.Value;
            Assert.That(Storage(deck).Cards.Count, Is.EqualTo(51));
            Assert.That(Storage(hand).Cards.Count, Is.EqualTo(2));
            Assert.That(SEntMan.GetComponent<PlayingCardsComponent>(hand).Kind, Is.EqualTo(PlayingCardKind.Hand));
            Execute(hand, "cards-verb-organize-up");
            Assert.That(Storage(hand).Cards.All(card => !card.FaceDown), Is.True);
            Execute(hand, "cards-verb-organize-down");
            Assert.That(Storage(hand).Cards.All(card => card.FaceDown), Is.True);
            Assert.That(SEntMan.System<PlayingCardsSystem>().TryTransfer(SPlayer, (hand, Storage(hand)),
                (deck, Storage(deck)), 2), Is.True);
        });
        await RunTicks(2);
        await Server.WaitAssertion(() =>
        {
            Assert.That(AllFaces(), Is.EqualTo(original));
            var order = Storage(deck).Cards.Select(c => c.Face.Id).ToArray();
            Server.ResolveDependency<IRobustRandom>().SetSeed(44);
            Execute(deck, "cards-verb-shuffle");
            Assert.That(Storage(deck).Cards.Select(c => c.Face.Id), Is.Not.EqualTo(order));
            Execute(deck, "cards-verb-organize-up");
            Execute(deck, "cards-verb-split");
            Assert.That(Storage(deck).Cards.Count, Is.EqualTo(27));
            var split = SEntMan.System<SharedHandsSystem>().GetActiveItem(SPlayer)!.Value;
            Assert.That(Storage(split).Cards.Count, Is.EqualTo(26));
            Execute(split, "arcane-cards-convert-to-hand");
            Execute(split, "cards-verb-flip");
            Assert.That(Storage(split).Cards.All(c => c.FaceDown), Is.True);
            Assert.That(SEntMan.System<PlayingCardsSystem>().TryTransfer(SPlayer, (deck, Storage(deck)),
                (split, Storage(split)), 1), Is.True);
            Assert.That(Storage(split).Cards.All(c => c.FaceDown), Is.True,
                "Adding a card revealed a hand concealed after splitting a face-up deck.");
            Assert.That(SEntMan.System<PlayingCardsSystem>().TryTransfer(SPlayer, (split, Storage(split)),
                (deck, Storage(deck)), 27), Is.True);
            Assert.That(SEntMan.System<PlayingCardsSystem>().TryTransfer(SPlayer, (split, Storage(split)),
                (deck, Storage(deck)), 27), Is.False, "A repeated transfer duplicated consumed cards.");
            Assert.That(AllFaces(), Is.EqualTo(original));
        });
    }

    [Test]
    public async Task PrivateHandRejectsStaleForgedAndDroppedRequests()
    {
        await SpawnTarget("CardDeckNanotrasen");
        await Activate();
        await RunSeconds(0.6f);
        await Activate();
        EntityUid hand = default;
        NetEntity[] audioChildren = [];
        await Server.WaitAssertion(() =>
        {
            hand = SEntMan.System<SharedHandsSystem>().GetActiveItem(SPlayer)!.Value;
            var ui = SEntMan.System<UserInterfaceSystem>();
            Assert.That(SEntMan.GetComponent<PlayingCardsComponent>(hand).Kind, Is.EqualTo(PlayingCardKind.Hand));
            Assert.That(SEntMan.GetComponent<ActorComponent>(SPlayer).PlayerSession.AttachedEntity, Is.EqualTo(SPlayer));
            Assert.That(ui.HasUi(hand, PlayingCardsUiKey.Key), Is.True);
            ui.OpenUi(hand, PlayingCardsUiKey.Key, SPlayer);
            Assert.That(ui.IsUiOpen(hand, PlayingCardsUiKey.Key, SPlayer), Is.True);
            var revision = Storage(hand).Revision;
            SendDraw(hand, revision, -1, SPlayer);
            SendDraw(hand, revision, int.MaxValue, SPlayer);
            var other = SEntMan.SpawnEntity("InteractionTestMob", Xform(SPlayer).Coordinates);
            SendDraw(hand, revision, 0, other);
            Assert.That(Storage(hand).Cards.Count, Is.EqualTo(2));
            Execute(hand, "cards-verb-shuffle");
            SendDraw(hand, revision, 0, SPlayer);
            Assert.That(Storage(hand).Cards.Count, Is.EqualTo(2), "A stale menu index selected a reordered card.");
            Assert.That(SEntMan.GetComponent<PlayingCardsComponent>(hand).VisibleStates, Is.All.EqualTo("back_nanotrasen"));
            audioChildren = GetServerAudioChildren(hand);
        });
        await RunTicks(10);
        var netHand = SEntMan.GetNetEntity(hand);
        await Client.WaitAssertion(() =>
        {
            var uid = CEntMan.GetEntity(netHand);
            var visual = CEntMan.GetComponent<PlayingCardsComponent>(uid);
            Assert.That(visual.Count, Is.EqualTo(2));
            Assert.That(visual.VisibleStates, Is.All.EqualTo("back_nanotrasen"));
            AssertClientChildrenAreKnownAudio(uid, audioChildren);
        });
        await DropAll();
        await Server.WaitAssertion(() =>
        {
            var ui = SEntMan.System<UserInterfaceSystem>();
            Assert.That(ui.IsUiOpen(hand, PlayingCardsUiKey.Key, SPlayer), Is.False);
            SendDraw(hand, Storage(hand).Revision, 0, SPlayer);
            ui.OpenUi(hand, PlayingCardsUiKey.Key, SPlayer);
            Assert.That(ui.IsUiOpen(hand, PlayingCardsUiKey.Key, SPlayer), Is.False, "A dropped hand disclosed its menu.");
            Assert.That(Storage(hand).Cards.Count, Is.EqualTo(2));
            Assert.That(AllFaces().Length, Is.EqualTo(53));
            Execute(hand, "cards-verb-convert-to-deck");
        });
        await RunTicks(10);
        await Client.WaitAssertion(() => Assert.That(CEntMan.HasComponent<PlayingCardDeckComponent>(CEntMan.GetEntity(netHand)),
            Is.True, "Converted decks do not satisfy the deck-box whitelist on the client."));
    }

    [Test]
    public async Task HiddenJokerKeepsItsServerEffectWithoutPublicComponentIdentity()
    {
        await SpawnTarget("CardDeckBlack");
        var deck = STarget!.Value;
        await Activate(); // The source's last ordered card is the Joker.
        EntityUid joker = default;
        NetEntity[] audioChildren = [];
        await Server.WaitAssertion(() =>
        {
            joker = SEntMan.System<SharedHandsSystem>().GetActiveItem(SPlayer)!.Value;
            Assert.That(Storage(joker).Cards.Single().Face.Id, Is.EqualTo("CardScJokerBlack"));
            Assert.That(SEntMan.HasComponent<EmbeddableProjectileComponent>(joker), Is.True);
            Assert.That(SEntMan.GetComponent<EmbeddableProjectileComponent>(joker).NetSyncEnabled, Is.False);
            var solution = SEntMan.GetComponent<SolutionComponent>(joker);
            Assert.That(solution.Solution.Volume, Is.EqualTo(FixedPoint2.New(4)));
            SEntMan.System<SharedSolutionContainerSystem>().SplitSolution((joker, solution), FixedPoint2.New(4));
            audioChildren = GetServerAudioChildren(joker);
        });
        await RunSeconds(0.6f);
        await Server.WaitAssertion(() => Assert.That(SEntMan.GetComponent<SolutionComponent>(joker).Solution.Volume,
            Is.EqualTo(FixedPoint2.New(4)), "The native regeneration did not refill the Joker."));
        var netJoker = SEntMan.GetNetEntity(joker);
        await Client.WaitAssertion(() =>
        {
            var uid = CEntMan.GetEntity(netJoker);
            Assert.That(CEntMan.GetComponent<MetaDataComponent>(uid).EntityPrototype!.ID, Is.EqualTo("CardBase"));
            Assert.That(CEntMan.GetComponent<PlayingCardsComponent>(uid).VisibleStates, Is.EqualTo(new[] { "back_black" }));
            Assert.That(CEntMan.HasComponent<EmbeddableProjectileComponent>(uid), Is.False);
            Assert.That(CEntMan.HasComponent<SolutionComponent>(uid), Is.False);
            AssertClientChildrenAreKnownAudio(uid, audioChildren);
        });
        await Activate();
        await Server.WaitAssertion(() =>
        {
            var hand = SEntMan.System<SharedHandsSystem>().GetActiveItem(SPlayer)!.Value;
            Assert.That(hand, Is.Not.EqualTo(joker));
            Assert.That(Storage(hand).Cards.Count, Is.EqualTo(2));
            Assert.That(SEntMan.HasComponent<EmbeddableProjectileComponent>(hand), Is.False,
                "A merged hand retained the materialized Joker's weapon components.");
            Assert.That(Storage(deck).Cards.Count, Is.EqualTo(51));
            Assert.That(AllFaces().Length, Is.EqualTo(53));
        });
    }

    private PlayingCardsStorageComponent Storage(EntityUid entity) => SEntMan.GetComponent<PlayingCardsStorageComponent>(entity);

    private NetEntity[] GetServerAudioChildren(EntityUid owner)
    {
        var children = SEntMan.AllEntities<TransformComponent>().Where(e => e.Comp.ParentUid == owner).ToArray();
        Assert.That(children.All(e => SEntMan.HasComponent<AudioComponent>(e.Owner)), Is.True,
            "A card or solution was materialized as a child entity.");
        return children.Select(e => SEntMan.GetNetEntity(e.Owner)).ToArray();
    }

    private void AssertClientChildrenAreKnownAudio(EntityUid owner, NetEntity[] audioChildren)
    {
        // Audio state is session-filtered, so a known audio entity can have only metadata and a transform here.
        var children = CEntMan.AllEntities<TransformComponent>().Where(e => e.Comp.ParentUid == owner)
            .Select(e => CEntMan.GetNetEntity(e.Owner));
        Assert.That(children, Is.SubsetOf(audioChildren), "An unexpected child entity reached the client.");
    }

    private string[] AllFaces() => SEntMan.AllEntities<PlayingCardsStorageComponent>()
        .Where(e => !SEntMan.IsQueuedForDeletion(e.Owner) && Xform(e.Owner).MapID == MapId)
        .SelectMany(e => e.Comp.Cards.Select(c => c.Face.Id)).Order().ToArray();

    private void Execute(EntityUid target, string key)
    {
        var verbs = SEntMan.System<SharedVerbSystem>();
        var verb = verbs.GetLocalVerbs(target, SPlayer, typeof(AlternativeVerb)).Single(v => v.Text == Loc.GetString(key));
        verbs.ExecuteVerb(verb, SPlayer, target);
    }

    private void SendDraw(EntityUid hand, int revision, int index, EntityUid actor)
    {
        var message = new PlayingCardsDrawMessage(revision, index) { Actor = actor, UiKey = PlayingCardsUiKey.Key };
        SEntMan.EventBus.RaiseLocalEvent(hand, message);
    }
}
// Arcane-End
