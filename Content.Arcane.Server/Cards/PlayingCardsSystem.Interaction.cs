using System.Linq;
using Content.Arcane.Shared.Cards;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Arcane.Server.Cards;

public sealed partial class PlayingCardsSystem
{
    private void OnUse(Entity<PlayingCardsStorageComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || !_hands.IsHolding(args.User, ent))
            return;
        Flip(args.User, ent);
        args.Handled = true;
    }

    private void OnActivate(Entity<PlayingCardsStorageComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex || !CanUse(args.User, ent))
            return;
        var active = _hands.GetActiveItem(args.User);
        if (active == ent.Owner)
            return;
        args.Handled = DrawInto(args.User, ent, active);
    }

    private bool DrawInto(EntityUid user, Entity<PlayingCardsStorageComponent> source, EntityUid? active)
    {
        if (active == null)
            return TryDraw(user, source, source.Comp.Cards.Count - 1);
        if (!TryComp<PlayingCardsStorageComponent>(active, out var target))
            return false;
        return TryTransfer(user, source, (active.Value, target), 1);
    }

    private void OnInteractUsing(Entity<PlayingCardsStorageComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || args.Used == ent.Owner || !TryComp<PlayingCardsStorageComponent>(args.Used, out var used))
            return;
        // Like source, using a card/hand on a pile deals one card into the held item; joining is a separate verb.
        args.Handled = DrawInto(args.User, ent, args.Used);
    }

    private void OnVerbs(Entity<PlayingCardsStorageComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;
        var user = args.User;
        args.Verbs.Add(new AlternativeVerb { Text = Loc.GetString("cards-verb-flip"), Act = () => Flip(user, ent) });
        if (args.Using is { } usingEntity && usingEntity != ent.Owner &&
            TryComp<PlayingCardsStorageComponent>(usingEntity, out var used))
        {
            args.Verbs.Add(new AlternativeVerb { Text = Loc.GetString("card-verb-join"), Priority = 8,
                Act = () => TryTransfer(user, (usingEntity, used), ent, used.Cards.Count) });
        }
        if (ent.Comp.Cards.Count < 2)
            return;
        args.Verbs.Add(new AlternativeVerb { Text = Loc.GetString("cards-verb-shuffle"), Act = () => Shuffle(user, ent) });
        args.Verbs.Add(new AlternativeVerb { Text = Loc.GetString("cards-verb-split"), Act = () => Split(user, ent) });
        args.Verbs.Add(new AlternativeVerb { Text = Loc.GetString("cards-verb-organize-down"), Act = () => Flip(user, ent, true) });
        args.Verbs.Add(new AlternativeVerb { Text = Loc.GetString("cards-verb-organize-up"), Act = () => Flip(user, ent, false) });
        var kind = Comp<PlayingCardsComponent>(ent).Kind;
        args.Verbs.Add(new AlternativeVerb {
            Text = Loc.GetString(kind == PlayingCardKind.Hand ? "cards-verb-convert-to-deck" : "arcane-cards-convert-to-hand"),
            Act = () => Convert(user, ent, kind == PlayingCardKind.Hand ? PlayingCardKind.Deck : PlayingCardKind.Hand) });
        if (kind == PlayingCardKind.Hand && _hands.IsHolding(user, ent))
            args.Verbs.Add(new AlternativeVerb { Text = Loc.GetString("cards-verb-pickcard"), Priority = 4,
                Act = () => { if (CanUse(user, ent) && _hands.IsHolding(user, ent)) _ui.OpenUi(ent.Owner, PlayingCardsUiKey.Key, user); } });
    }

    private void OnActivationVerbs(Entity<PlayingCardsStorageComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || args.Using == ent.Owner)
            return;
        var user = args.User;
        var usingEntity = args.Using;
        args.Verbs.Add(new ActivationVerb { Text = Loc.GetString("cards-verb-draw"), Priority = 16,
            Act = () => DrawInto(user, ent, usingEntity) });
    }

    private bool CanView(Entity<PlayingCardsStorageComponent> ent, EntityUid actor)
        => ent.Comp.Viewer == actor && CanUse(actor, ent) && _hands.IsHolding(actor, ent) &&
           Comp<PlayingCardsComponent>(ent).Kind == PlayingCardKind.Hand &&
           TryComp<ActorComponent>(actor, out var account) && account.PlayerSession.UserId == ent.Comp.ViewerId &&
           account.PlayerSession.AttachedEntity == actor && _ui.IsUiOpen(ent.Owner, PlayingCardsUiKey.Key, actor);

    private void OnUiOpened(Entity<PlayingCardsStorageComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(PlayingCardsUiKey.Key))
            return;
        if (!CanUse(args.Actor, ent) || !_hands.IsHolding(args.Actor, ent) ||
            Comp<PlayingCardsComponent>(ent).Kind != PlayingCardKind.Hand || !TryComp<ActorComponent>(args.Actor, out var actor))
        {
            _ui.CloseUi(ent.Owner, PlayingCardsUiKey.Key, args.Actor);
            return;
        }
        ent.Comp.Viewer = args.Actor;
        ent.Comp.ViewerId = actor.PlayerSession.UserId;
        _viewed.Add(ent);
        SendSnapshot(ent, args.Actor);
    }

    private void SendSnapshot(Entity<PlayingCardsStorageComponent> ent, EntityUid actor)
    {
        if (!CanView(ent, actor))
        {
            CloseViewer(ent);
            return;
        }
        var cards = new List<PlayingCardFace>(ent.Comp.Cards.Count);
        foreach (var record in ent.Comp.Cards)
        {
            var face = _prototypes.Index(record.Face);
            cards.Add(new PlayingCardFace(face.State, face.Name));
        }
        _ui.ServerSendUiMessage(ent.Owner, PlayingCardsUiKey.Key,
            new PlayingCardsSnapshotMessage(ent.Comp.Revision, cards), actor);
    }

    private void OnRefresh(Entity<PlayingCardsStorageComponent> ent, ref PlayingCardsRefreshMessage args)
    {
        if (CanView(ent, args.Actor))
            SendSnapshot(ent, args.Actor);
    }

    private void OnDrawMessage(Entity<PlayingCardsStorageComponent> ent, ref PlayingCardsDrawMessage args)
    {
        if (!CanView(ent, args.Actor))
            return;
        if (ent.Comp.Revision != args.Revision)
        {
            SendSnapshot(ent, args.Actor);
            return;
        }
        TryDraw(args.Actor, ent, args.Index);
    }

    private void CloseViewer(Entity<PlayingCardsStorageComponent> ent)
    {
        var actor = ent.Comp.Viewer;
        ent.Comp.Viewer = null;
        ent.Comp.ViewerId = null;
        _viewed.Remove(ent);
        if (actor != null)
            _ui.CloseUi(ent.Owner, PlayingCardsUiKey.Key, actor);
    }

    private void OnUiClosed(Entity<PlayingCardsStorageComponent> ent, ref BoundUIClosedEvent args)
    {
        if (args.UiKey.Equals(PlayingCardsUiKey.Key) && ent.Comp.Viewer == args.Actor)
        {
            ent.Comp.Viewer = null;
            ent.Comp.ViewerId = null;
            _viewed.Remove(ent);
        }
    }

    private void OnDropped(Entity<PlayingCardsStorageComponent> ent, ref GotUnequippedHandEvent args) => CloseViewer(ent);
    private void OnShutdown(Entity<PlayingCardsStorageComponent> ent, ref ComponentShutdown args) => CloseViewer(ent);

    public override void Update(float frameTime)
    {
        if (_timing.CurTime < _nextViewerCheck)
            return;
        _nextViewerCheck = _timing.CurTime + TimeSpan.FromSeconds(0.5);
        foreach (var uid in _viewed.ToArray())
            if (TryComp<PlayingCardsStorageComponent>(uid, out var storage) &&
                (storage.Viewer is not { } actor || !CanView((uid, storage), actor)))
                CloseViewer((uid, storage));
    }
}
