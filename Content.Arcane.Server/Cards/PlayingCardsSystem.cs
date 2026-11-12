using System.Linq;
using Content.Arcane.Shared.Cards;
using Content.Shared.ActionBlocker;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Cards;

public sealed partial class PlayingCardsSystem : EntitySystem
{
    // Source supports four complete 53-card decks. Hand/deck render limits are not capacities.
    public const int MaxCards = 212;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedItemSystem _items = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    private readonly HashSet<EntityUid> _viewed = [];
    private TimeSpan _nextViewerCheck;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayingCardsStorageComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<PlayingCardsStorageComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<PlayingCardsStorageComponent, UseInHandEvent>(OnUse);
        SubscribeLocalEvent<PlayingCardsStorageComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<PlayingCardsStorageComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<PlayingCardsStorageComponent, GetVerbsEvent<AlternativeVerb>>(OnVerbs);
        SubscribeLocalEvent<PlayingCardsStorageComponent, GetVerbsEvent<ActivationVerb>>(OnActivationVerbs);
        SubscribeLocalEvent<PlayingCardsStorageComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<PlayingCardsStorageComponent, PlayingCardsRefreshMessage>(OnRefresh);
        SubscribeLocalEvent<PlayingCardsStorageComponent, PlayingCardsDrawMessage>(OnDrawMessage);
        SubscribeLocalEvent<PlayingCardsStorageComponent, BoundUIClosedEvent>(OnUiClosed);
        SubscribeLocalEvent<PlayingCardsStorageComponent, GotUnequippedHandEvent>(OnDropped);
        SubscribeLocalEvent<PlayingCardsStorageComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnMapInit(Entity<PlayingCardsStorageComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Cards.Count == 0)
        {
            foreach (var id in ent.Comp.Content.Take(MaxCards))
                ent.Comp.Cards.Add(new PlayingCardRecord { Face = id, BackState = ent.Comp.BackState, FaceDown = ent.Comp.FaceDown });
        }
        ent.Comp.Content.Clear();
        Refresh(ent);
    }

    private void OnExamined(Entity<PlayingCardsStorageComponent> ent, ref ExaminedEvent args)
    {
        args.PushText(Loc.GetString("card-stack-examine", ("count", ent.Comp.Cards.Count)));
        if (args.IsInDetailsRange && ent.Comp.Cards.Count == 1 && !ent.Comp.Cards[0].FaceDown)
            args.PushText(Loc.GetString("card-examined", ("target", Loc.GetString(_prototypes.Index(ent.Comp.Cards[0].Face).Name))));
    }

    private bool CanUse(EntityUid user, EntityUid target)
        => !TerminatingOrDeleted(user) && !TerminatingOrDeleted(target) &&
           HasComp<HandsComponent>(user) &&
           !EntityManager.IsQueuedForDeletion(user) && !EntityManager.IsQueuedForDeletion(target) &&
           _blocker.CanInteract(user, target) && _interaction.IsAccessible(user, target) &&
           _interaction.InRangeUnobstructed(user, target);

    private void Refresh(Entity<PlayingCardsStorageComponent> ent)
    {
        var visual = Comp<PlayingCardsComponent>(ent);
        visual.Count = ent.Comp.Cards.Count;
        if (visual.Count == 1)
            visual.Kind = PlayingCardKind.Single;
        var count = visual.Kind switch { PlayingCardKind.Hand => 10, PlayingCardKind.Deck => 5, _ => 1 };
        visual.VisibleStates = ent.Comp.Cards.TakeLast(count)
            .Select(card => card.FaceDown ? card.BackState : _prototypes.Index(card.Face).State).ToList();
        ent.Comp.Revision++;
        Dirty(ent, visual);
        _items.SetSize(ent, visual.Kind == PlayingCardKind.Deck ? "Normal" : "Small");
        if (visual.Kind == PlayingCardKind.Deck)
            EnsureComp<PlayingCardDeckComponent>(ent);
        else
            RemComp<PlayingCardDeckComponent>(ent);
        if (visual.Count == 1 && !ent.Comp.HasFaceComponents)
        {
            var components = _prototypes.Index(ent.Comp.Cards[0].Face).Components;
            if (components.Count > 0)
            {
                EntityManager.AddComponents(ent, components, removeExisting: false);
                ent.Comp.HasFaceComponents = true;
            }
        }
        if (ent.Comp.Viewer is { } viewer)
            SendSnapshot(ent, viewer);
    }

    /// <summary>One authoritative mutation; hidden records never have network entity identities.</summary>
    public bool TryTransfer(EntityUid user, EntityUid sourceUid, EntityUid targetUid, int count)
    {
        if (!TryComp<PlayingCardsStorageComponent>(sourceUid, out var sourceStorage) ||
            !TryComp<PlayingCardsStorageComponent>(targetUid, out var targetStorage))
            return false;
        Entity<PlayingCardsStorageComponent> source = (sourceUid, sourceStorage);
        Entity<PlayingCardsStorageComponent> target = (targetUid, targetStorage);
        if (source.Owner == target.Owner || !CanUse(user, source) || !CanUse(user, target) ||
            count <= 0 || count > source.Comp.Cards.Count || target.Comp.Cards.Count + count > MaxCards)
            return false;

        BaseContainer? previousContainer = null;
        if (target.Comp.HasFaceComponents)
        {
            // A single special card has server-only effects. Retire its entity when it joins a hand.
            if (_containers.TryGetContainingContainer(target.Owner, out previousContainer) &&
                !_containers.Remove(target.Owner, previousContainer))
                return false;

            var replacement = SpawnNextToOrDrop("CardBase", target.Owner);
            var storage = Comp<PlayingCardsStorageComponent>(replacement);
            storage.Cards.AddRange(target.Comp.Cards);
            storage.BackState = target.Comp.BackState;
            storage.FaceDown = target.Comp.FaceDown;
            storage.ShuffleSound = target.Comp.ShuffleSound;
            storage.PickupSound = target.Comp.PickupSound;
            storage.PlaceSound = target.Comp.PlaceSound;
            target.Comp.Cards.Clear();
            FinishRemoval(target);
            target = (replacement, storage);
        }

        var cards = source.Comp.Cards.GetRange(source.Comp.Cards.Count - count, count);
        source.Comp.Cards.RemoveRange(source.Comp.Cards.Count - count, count);
        var targetVisual = Comp<PlayingCardsComponent>(target);
        if (targetVisual.Kind == PlayingCardKind.Single && target.Comp.Cards.Count > 0)
        {
            targetVisual.Kind = PlayingCardKind.Hand;
            target.Comp.FaceDown = target.Comp.Cards[0].FaceDown;
        }
        target.Comp.Cards.AddRange(cards);
        if (Comp<PlayingCardsComponent>(target).Kind == PlayingCardKind.Hand)
            foreach (var card in target.Comp.Cards)
                card.FaceDown = target.Comp.FaceDown;
        Refresh(target);
        if (previousContainer != null)
            _containers.Insert(target.Owner, previousContainer);
        FinishRemoval(source);
        _audio.PlayPvs(target.Comp.PlaceSound, target);
        return true;
    }

    private void FinishRemoval(Entity<PlayingCardsStorageComponent> ent)
    {
        if (ent.Comp.Cards.Count == 0)
        {
            CloseViewer(ent);
            QueueDel(ent);
        }
        else
            Refresh(ent);
    }

    public bool TryDraw(EntityUid user, EntityUid sourceUid, int index)
    {
        if (!TryComp<PlayingCardsStorageComponent>(sourceUid, out var sourceStorage))
            return false;
        Entity<PlayingCardsStorageComponent> source = (sourceUid, sourceStorage);
        if (!CanUse(user, source) || index < 0 || index >= source.Comp.Cards.Count)
            return false;
        if (source.Comp.Cards.Count == 1)
            return _hands.TryPickupAnyHand(user, source);
        var record = source.Comp.Cards[index];
        var drawn = Spawn("CardBase", Transform(source).Coordinates);
        var storage = Comp<PlayingCardsStorageComponent>(drawn);
        storage.Cards.Add(record);
        source.Comp.Cards.RemoveAt(index);
        Refresh((drawn, storage));
        FinishRemoval(source);
        _hands.PickupOrDrop(user, drawn);
        _audio.PlayPvs(source.Comp.PickupSound, drawn);
        return true;
    }

    private void Flip(EntityUid user, Entity<PlayingCardsStorageComponent> ent, bool? faceDown = null)
    {
        if (!CanUse(user, ent))
            return;
        ent.Comp.FaceDown = faceDown ?? !ent.Comp.FaceDown;
        foreach (var card in ent.Comp.Cards)
            card.FaceDown = faceDown ?? !card.FaceDown;
        Refresh(ent);
    }

    private void Shuffle(EntityUid user, Entity<PlayingCardsStorageComponent> ent)
    {
        if (!CanUse(user, ent))
            return;
        _random.Shuffle(ent.Comp.Cards);
        Refresh(ent);
        _audio.PlayPvs(ent.Comp.ShuffleSound, ent);
    }

    private void Split(EntityUid user, Entity<PlayingCardsStorageComponent> ent)
    {
        if (!CanUse(user, ent) || ent.Comp.Cards.Count < 2)
            return;
        var pile = Spawn("CardDeckBase", Transform(ent).Coordinates);
        if (!TryTransfer(user, ent, pile, ent.Comp.Cards.Count / 2))
        {
            QueueDel(pile);
            return;
        }
        _hands.PickupOrDrop(user, pile);
    }

    private void Convert(EntityUid user, Entity<PlayingCardsStorageComponent> ent, PlayingCardKind kind)
    {
        if (!CanUse(user, ent) || ent.Comp.Cards.Count < 2)
            return;
        Comp<PlayingCardsComponent>(ent).Kind = kind;
        if (kind == PlayingCardKind.Hand)
        {
            ent.Comp.FaceDown = ent.Comp.Cards[^1].FaceDown;
            foreach (var card in ent.Comp.Cards)
                card.FaceDown = ent.Comp.FaceDown;
        }
        Refresh(ent);
    }
}
