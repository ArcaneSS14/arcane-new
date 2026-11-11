using Content.Arcane.Shared.Paint;
using Content.Server.Power.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Arcane.Server.Paint;

public sealed partial class PhotocopierSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private PaintLogSystem _paintLog = default!;
    [Dependency] private PaintSurfaceSystem _paint = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PowerReceiverSystem _power = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PhotocopierComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<PhotocopierComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<PhotocopierComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<PhotocopierComponent, EntRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<PhotocopierComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PhotocopierComponent, PhotocopierDoAfterEvent>(OnDoAfter);
        SubscribeLocalEvent<PhotocopierComponent, DoAfterAttemptEvent<PhotocopierDoAfterEvent>>(OnAttempt);
    }

    private void OnActivate(Entity<PhotocopierComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        args.Handled = true;
        TryStart(ent, args.User);
    }

    private void OnGetVerbs(Entity<PhotocopierComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.State != PhotocopierState.Idle)
            return;

        var user = args.User;
        args.Verbs.Add(new Verb { Text = Loc.GetString("photocopier-verb-copy"), Act = () => TryStart(ent, user) });
    }

    private void OnPowerChanged(Entity<PhotocopierComponent> ent, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            Reset(ent);
    }

    private void OnRemoved(Entity<PhotocopierComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID == ent.Comp.FirstSlot || args.Container.ID == ent.Comp.SecondSlot)
            Reset(ent);
    }

    private void OnShutdown(Entity<PhotocopierComponent> ent, ref ComponentShutdown args) => Reset(ent);

    public bool TryStart(Entity<PhotocopierComponent> ent, EntityUid user)
    {
        if (TerminatingOrDeleted(ent) || EntityManager.IsQueuedForDeletion(ent)
            || TerminatingOrDeleted(user) || EntityManager.IsQueuedForDeletion(user)
            || !_blocker.CanInteract(user, ent)
            || !_interaction.IsAccessible(user, ent.Owner)
            || !_interaction.InRangeUnobstructed(user, ent.Owner))
            return false;

        if (ent.Comp.State != PhotocopierState.Idle)
            return Fail(ent, user, "photocopier-busy");

        if (!_power.IsPowered(ent))
            return Fail(ent, user, "photocopier-unpowered");

        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.FirstSlot, out var firstSlot)
            || !_itemSlots.TryGetSlot(ent.Owner, ent.Comp.SecondSlot, out var secondSlot)
            || firstSlot.Locked || secondSlot.Locked)
            return false;

        var first = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.FirstSlot);
        var second = _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.SecondSlot);
        if (first is not { } a || second is not { } b || a == b
            || !HasComp<PaintCanvasVisualsComponent>(a) || !HasComp<PaintCanvasVisualsComponent>(b))
            return Fail(ent, user, "photocopier-needs-canvases");

        var aBlank = IsBlank(a);
        var bBlank = IsBlank(b);
        if (aBlank == bBlank)
            return Fail(ent, user, aBlank ? "photocopier-nothing-to-copy" : "photocopier-needs-blank");

        var original = aBlank ? b : a;
        var target = aBlank ? a : b;
        if (!TryComp<PaintSurfaceComponent>(original, out var originalSurface)
            || !TryComp<PaintSurfaceComponent>(target, out var targetSurface)
            || originalSurface.Size != targetSurface.Size
            || originalSurface.Image is not { } image || !image.IsValid())
            return Fail(ent, user, "photocopier-size-mismatch");

        ent.Comp.Original = original;
        ent.Comp.Target = target;
        ent.Comp.User = user;
        ent.Comp.FirstCanvas = a;
        ent.Comp.SecondCanvas = b;
        ent.Comp.Operation++;
        SetLocked(ent, true);
        if (!StartStage(ent, PhotocopierState.Scanning))
        {
            Reset(ent);
            return false;
        }

        return true;
    }

    private bool StartStage(Entity<PhotocopierComponent> ent, PhotocopierState stage)
    {
        ent.Comp.State = stage;
        var args = new DoAfterArgs(EntityManager, ent,
            stage == PhotocopierState.Scanning ? ent.Comp.ScanTime : ent.Comp.PrintTime,
            new PhotocopierDoAfterEvent { Operation = ent.Comp.Operation, Stage = stage },
            ent, ent.Comp.Original, ent.Comp.Target)
        {
            NeedHand = false,
            BreakOnMove = false,
            RequireCanInteract = false,
            DistanceThreshold = null,
            AttemptFrequency = AttemptFrequency.EveryTick,
            BlockDuplicate = false,
            CancelDuplicate = false,
        };
        if (!_doAfter.TryStartDoAfter(args, out var id))
            return false;

        ent.Comp.DoAfter = id;
        _appearance.SetData(ent, PhotocopierVisuals.State, stage);
        _audio.PlayPvs(stage == PhotocopierState.Scanning ? ent.Comp.ScanSound : ent.Comp.PrintSound, ent);
        return true;
    }

    private void OnAttempt(Entity<PhotocopierComponent> ent, ref DoAfterAttemptEvent<PhotocopierDoAfterEvent> args)
    {
        if (!IsCurrent(ent, args.Event) || !ValidContents(ent))
            args.Cancel();
    }

    private bool IsCurrent(Entity<PhotocopierComponent> ent, PhotocopierDoAfterEvent ev) =>
        ent.Comp.State != PhotocopierState.Idle && ent.Comp.State == ev.Stage && ent.Comp.Operation == ev.Operation;

    private bool ValidContents(Entity<PhotocopierComponent> ent)
    {
        return !TerminatingOrDeleted(ent) && !EntityManager.IsQueuedForDeletion(ent) && _power.IsPowered(ent)
            && ent.Comp.Original is { } original && ent.Comp.Target is { } target
            && !TerminatingOrDeleted(original) && !EntityManager.IsQueuedForDeletion(original)
            && !TerminatingOrDeleted(target) && !EntityManager.IsQueuedForDeletion(target)
            && _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.FirstSlot) == ent.Comp.FirstCanvas
            && _itemSlots.GetItemOrNull(ent.Owner, ent.Comp.SecondSlot) == ent.Comp.SecondCanvas
            && TryComp<PaintSurfaceComponent>(original, out var originalSurface)
            && TryComp<PaintSurfaceComponent>(target, out var targetSurface)
            && originalSurface.Size == targetSurface.Size
            && originalSurface.Image is { } image && image.IsValid() && IsBlank(target);
    }

    private void OnDoAfter(Entity<PhotocopierComponent> ent, ref PhotocopierDoAfterEvent args)
    {
        if (args.Handled || !IsCurrent(ent, args))
            return;

        args.Handled = true;
        ent.Comp.DoAfter = null;
        if (args.Cancelled || !ValidContents(ent))
        {
            Reset(ent);
            return;
        }

        if (args.Stage == PhotocopierState.Scanning)
        {
            if (!StartStage(ent, PhotocopierState.Printing))
                Reset(ent);
            return;
        }

        var original = ent.Comp.Original!.Value;
        var target = ent.Comp.Target!.Value;
        var user = ent.Comp.User;
        var source = Comp<PaintSurfaceComponent>(original);
        var destination = Comp<PaintSurfaceComponent>(target);
        var copy = source.Image!.Clone();
        if (_paint.SetImage((target, destination), copy))
        {
            var title = source.Title ?? Loc.GetString("paint-untitled");
            var artist = source.Artist is { } name ? Loc.GetString("paint-copy-artist", ("artist", name)) : null;
            _paint.SetFinished((target, destination), title, artist, "paint-copy-name");
            _paintLog.LogCopied(original, target, user, copy);
        }

        Reset(ent);
    }

    private void Reset(Entity<PhotocopierComponent> ent)
    {
        if (ent.Comp.State == PhotocopierState.Idle)
            return;

        var id = ent.Comp.DoAfter;
        ent.Comp.DoAfter = null;
        ent.Comp.State = PhotocopierState.Idle;
        ent.Comp.Original = null;
        ent.Comp.Target = null;
        ent.Comp.User = null;
        ent.Comp.FirstCanvas = null;
        ent.Comp.SecondCanvas = null;
        _doAfter.Cancel(id);
        SetLocked(ent, false);
        _appearance.SetData(ent, PhotocopierVisuals.State, PhotocopierState.Idle);
    }

    private void SetLocked(Entity<PhotocopierComponent> ent, bool locked)
    {
        _itemSlots.SetLock(ent.Owner, ent.Comp.FirstSlot, locked);
        _itemSlots.SetLock(ent.Owner, ent.Comp.SecondSlot, locked);
    }

    private bool IsBlank(EntityUid uid)
    {
        if (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid)
            || !TryComp<PaintSurfaceComponent>(uid, out var surface)
            || surface.Finished || surface.Image is not { } image || !image.IsValid())
            return false;

        foreach (var pixel in image.Pixels)
        {
            if (pixel != 0)
                return false;
        }
        return true;
    }

    private bool Fail(EntityUid machine, EntityUid user, string key)
    {
        _popup.PopupEntity(Loc.GetString(key), machine, user);
        return false;
    }
}
