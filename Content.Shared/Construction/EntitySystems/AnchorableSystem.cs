using Content.Shared.Administration.Logs;
using Content.Shared.Examine;
using Content.Shared.Construction.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Station.Systems;
using Content.Shared.Tools.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Shared.Construction.EntitySystems;

public sealed partial class AnchorableSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private StationSystem _stationSystem = null!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private TagSystem _tagSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    [Dependency] private EntityQuery<PhysicsComponent> _physicsQuery = default!;

    public readonly ProtoId<TagPrototype> Unstackable = "Unstackable";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnchorableComponent, InteractUsingEvent>(OnInteractUsing,
            before: new[] { typeof(ItemSlotsSystem) }, after: new[] { typeof(SharedConstructionSystem) });
        SubscribeLocalEvent<AnchorableComponent, TryAnchorCompletedEvent>(OnAnchorComplete);
        SubscribeLocalEvent<AnchorableComponent, TryUnanchorCompletedEvent>(OnUnanchorComplete);
        SubscribeLocalEvent<AnchorableComponent, ExaminedEvent>(OnAnchoredExamine);
        SubscribeLocalEvent<AnchorableComponent, ComponentStartup>(OnAnchorStartup);
        SubscribeLocalEvent<AnchorableComponent, AnchorStateChangedEvent>(OnAnchorStateChange);

        SubscribeLocalEvent<AnchorOnlyOnStationComponent, AnchorAttemptEvent>(OnAnchorOnStation);
    }

    private void OnAnchorStartup(EntityUid uid, AnchorableComponent comp, ComponentStartup args)
    {
        _appearance.SetData(uid, AnchorVisuals.Anchored, Transform(uid).Anchored);
    }

    private void OnAnchorOnStation(Entity<AnchorOnlyOnStationComponent> ent, ref AnchorAttemptEvent args)
    {
        if (_stationSystem.IsOnStation(ent, ent.Comp.OnlyCountLargestGrid))
            return;

        args.FailMessage = Loc.GetString(ent.Comp.PopupMessageAnchorFail);
        args.Cancel();
    }

    private void OnAnchorStateChange(EntityUid uid, AnchorableComponent comp, AnchorStateChangedEvent args)
    {
        _appearance.SetData(uid, AnchorVisuals.Anchored, args.Anchored);
    }

    /// <summary>
    ///     Tries to unanchor the entity.
    /// </summary>
    /// <returns>true if unanchored, false otherwise</returns>
    private void TryUnAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
        AnchorableComponent? anchorable = null,
        TransformComponent? transform = null,
        ToolComponent? usingTool = null)
    {
        if (!Resolve(uid, ref anchorable, ref transform))
            return;

        if (!Resolve(usingUid, ref usingTool))
            return;

        if (!Valid(uid, userUid, usingUid, false, out var failMessage, out var actualDelay))
        {
            if (failMessage != null)
                _popup.PopupEntity(failMessage, uid, userUid);
            return;
        }

        // Log unanchor attempt (server only)
        _adminLogger.Add(LogType.Anchor, LogImpact.Low, $"{ToPrettyString(userUid):user} is trying to unanchor {ToPrettyString(uid):entity} from {transform.Coordinates:targetlocation}");

        _tool.UseTool(usingUid, userUid, uid, actualDelay, usingTool.Qualities, new TryUnanchorCompletedEvent());
    }

    private void OnInteractUsing(EntityUid uid, AnchorableComponent anchorable, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // If the used entity doesn't have a tool, return early.
        if (!TryComp(args.Used, out ToolComponent? usedTool) || !_tool.HasQuality(args.Used, anchorable.Tool, usedTool))
            return;

        args.Handled = true;
        TryToggleAnchor(uid, args.User, args.Used, anchorable, usingTool: usedTool);
    }

    private void OnAnchoredExamine(EntityUid uid, AnchorableComponent component, ExaminedEvent args)
    {
        var isAnchored = Comp<TransformComponent>(uid).Anchored;

        if (isAnchored && (component.Flags & AnchorableFlags.Unanchorable) == 0x0)
            return;

        if (!isAnchored && (component.Flags & AnchorableFlags.Anchorable) == 0x0)
            return;

        var messageId = isAnchored ? "examinable-anchored" : "examinable-unanchored";
        args.PushMarkup(Loc.GetString(messageId, ("target", uid)));
    }

    private void OnUnanchorComplete(EntityUid uid, AnchorableComponent component, TryUnanchorCompletedEvent args)
    {
        // Arcane-Edit-Start
        if (args.Cancelled || args.Used is not { } used)
            return;

        CompleteUnanchor(uid, args.User, used);
        // Arcane-Edit-End
    }

    private void OnAnchorComplete(EntityUid uid, AnchorableComponent component, TryAnchorCompletedEvent args)
    {
        // Arcane-Edit-Start
        if (args.Cancelled || args.Used is not { } used)
            return;

        CompleteAnchor(uid, args.User, used, component);
        // Arcane-Edit-End
    }

    // Arcane-Start
    public bool TryAnchor(EntityUid uid, EntityUid userUid)
    {
        AnchorableComponent? anchorable = null;
        TransformComponent? transform = null;
        if (!Resolve(uid, ref anchorable, ref transform))
            return false;

        if (transform.Anchored)
            return false;

        if (!Valid(uid, userUid, userUid, true, out var failMessage, out _, anchorable, checkTool: false))
        {
            if (failMessage != null)
                _popup.PopupEntity(Loc.GetString(failMessage), uid, userUid);
            return false;
        }

        if (transform.Anchored || (anchorable.Flags & AnchorableFlags.Anchorable) == 0x0)
            return false;

        return CompleteAnchor(uid, userUid, userUid, anchorable, transform);
    }

    public bool TryGetManualAnchoringDelay(EntityUid uid, EntityUid userUid, bool anchoring, out TimeSpan delay)
    {
        delay = TimeSpan.Zero;
        AnchorableComponent? anchorable = null;
        TransformComponent? transform = null;
        if (!Resolve(uid, ref anchorable, ref transform) || transform.Anchored == anchoring)
            return false;

        if (!Valid(uid, userUid, userUid, anchoring, out var failMessage, out var actualDelay, anchorable, checkTool: false))
        {
            if (failMessage != null)
                _popup.PopupEntity(Loc.GetString(failMessage), uid, userUid);
            return false;
        }

        delay = TimeSpan.FromSeconds(actualDelay);
        return true;
    }

    public bool CompleteManualAnchoring(EntityUid uid, EntityUid userUid, bool anchoring)
    {
        AnchorableComponent? anchorable = null;
        TransformComponent? transform = null;
        if (!Resolve(uid, ref anchorable, ref transform) || transform.Anchored == anchoring)
            return false;

        if ((anchoring && (anchorable.Flags & AnchorableFlags.Anchorable) == 0x0) ||
            (!anchoring && (anchorable.Flags & AnchorableFlags.Unanchorable) == 0x0))
        {
            return false;
        }

        if (!Valid(uid, userUid, userUid, anchoring, out var failMessage, out _, anchorable, checkTool: false))
        {
            if (failMessage != null)
                _popup.PopupEntity(Loc.GetString(failMessage), uid, userUid);
            return false;
        }

        return anchoring
            ? CompleteAnchor(uid, userUid, userUid, anchorable, transform)
            : CompleteUnanchor(uid, userUid, userUid, anchorable, transform);
    }

    public bool TryUnanchor(EntityUid uid, EntityUid userUid)
    {
        AnchorableComponent? anchorable = null;
        TransformComponent? transform = null;
        if (!Resolve(uid, ref anchorable, ref transform))
            return false;

        if (!transform.Anchored)
            return false;

        if (!Valid(uid, userUid, userUid, false, out var failMessage, out _, anchorable, checkTool: false))
        {
            if (failMessage != null)
                _popup.PopupEntity(Loc.GetString(failMessage), uid, userUid);
            return false;
        }

        if (!transform.Anchored || (anchorable.Flags & AnchorableFlags.Unanchorable) == 0x0)
            return false;

        return CompleteUnanchor(uid, userUid, userUid, anchorable, transform);
    }

    private bool CompleteUnanchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
        AnchorableComponent? anchorable = null, TransformComponent? transform = null)
    {
        if (!Resolve(uid, ref anchorable, ref transform))
            return false;

        RaiseLocalEvent(uid, new BeforeUnanchoredEvent(userUid, usingUid));
        _transformSystem.Unanchor(uid, transform);
        RaiseLocalEvent(uid, new UserUnanchoredEvent(userUid, usingUid));

        _popup.PopupEntity(Loc.GetString("anchorable-unanchored"), uid, userUid);

        _adminLogger.Add(
            LogType.Unanchor,
            LogImpact.Low,
            $"{ToPrettyString(userUid):user} unanchored {ToPrettyString(uid):anchored} using {ToPrettyString(usingUid):using}"
        );
        return true;
    }

    private bool CompleteAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
        AnchorableComponent? anchorable = null, TransformComponent? transform = null)
    {
        if (!Resolve(uid, ref anchorable, ref transform))
            return false;

        if (TryComp<PhysicsComponent>(uid, out var anchorBody) &&
            !TileFree(transform.Coordinates, anchorBody))
        {
            _popup.PopupEntity(Loc.GetString("anchorable-occupied"), uid, userUid);
            return false;
        }

        var rot = Math.Round(transform.LocalRotation / (Math.PI / 2)) * (Math.PI / 2);

        if (TryComp<PullableComponent>(uid, out var pullable) && pullable.Puller != null)
            _pulling.TryStopPull(uid, pullable, ignoreGrab: true); // Trauma

        if (anchorable.Snap)
        {
            var coordinates = transform.Coordinates.SnapToGrid(EntityManager);

            if (AnyUnstackable(uid, coordinates))
            {
                _popup.PopupEntity(Loc.GetString("construction-step-condition-no-unstackable-in-tile"), uid, userUid);
                return false;
            }

            _transformSystem.SetCoordinates(uid, transform, coordinates, rotation: rot);
        }
        else
        {
            _transformSystem.SetLocalRotationNoLerp(uid, rot, transform);
        }

        RaiseLocalEvent(uid, new BeforeAnchoredEvent(userUid, usingUid));

        if (!transform.Anchored)
            _transformSystem.AnchorEntity(uid, transform);

        RaiseLocalEvent(uid, new UserAnchoredEvent(userUid, usingUid));

        _popup.PopupEntity(Loc.GetString("anchorable-anchored"), uid, userUid);

        _adminLogger.Add(
            LogType.Anchor,
            LogImpact.Low,
            $"{ToPrettyString(userUid):user} anchored {ToPrettyString(uid):anchored} using {ToPrettyString(usingUid):using}"
        );
        return true;
    }
    // Arcane-End

    /// <summary>
    ///     Tries to toggle the anchored status of this component's owner.
    ///     override is used due to popup and adminlog being server side systems in this case.
    /// </summary>
    /// <returns>true if toggled, false otherwise</returns>
    public void TryToggleAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
        AnchorableComponent? anchorable = null,
        TransformComponent? transform = null,
        PullableComponent? pullable = null,
        ToolComponent? usingTool = null)
    {
        if (!Resolve(uid, ref transform))
            return;

        if (transform.Anchored)
        {
            TryUnAnchor(uid, userUid, usingUid, anchorable, transform, usingTool);
        }
        else
        {
            TryAnchor(uid, userUid, usingUid, anchorable, transform, pullable, usingTool);
        }
    }

    /// <summary>
    ///     Tries to anchor the entity.
    /// </summary>
    /// <returns>true if anchored, false otherwise</returns>
    private void TryAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
            AnchorableComponent? anchorable = null,
            TransformComponent? transform = null,
            PullableComponent? pullable = null,
            ToolComponent? usingTool = null)
    {
        if (!Resolve(uid, ref anchorable, ref transform))
            return;

        // Optional resolves.
        Resolve(uid, ref pullable, false);

        if (!Resolve(usingUid, ref usingTool))
            return;

        if (!Valid(uid, userUid, usingUid, true, out var failMessage, out var actualDelay, anchorable, usingTool))
        {
            if (failMessage != null)
                _popup.PopupEntity(Loc.GetString(failMessage), uid, userUid);
            return;
        }

        // Log anchor attempt (server only)
        _adminLogger.Add(LogType.Anchor, LogImpact.Low, $"{ToPrettyString(userUid):user} is trying to anchor {ToPrettyString(uid):entity} to {transform.Coordinates:targetlocation}");

        if (!CanAnchorAt(uid, transform.Coordinates, userUid))
            return;

        if (AnyUnstackable(uid, transform.Coordinates))
        {
            _popup.PopupEntity(Loc.GetString("construction-step-condition-no-unstackable-in-tile"), uid, userUid);
            return;
        }

        _tool.UseTool(usingUid, userUid, uid, actualDelay, usingTool.Qualities, new TryAnchorCompletedEvent());
    }

    /// <summary>
    /// Checks if a given entity can be (un)anchored by a particular user and tool.
    /// </summary>
    /// <param name="uid">The target entity to be (un)anchored.</param>
    /// <param name="userUid">The user attempting to anchor it.</param>
    /// <param name="usingUid">The tool being used to (un)anchor the target entity.</param>
    /// <param name="anchoring">True if the target will be anchored, false if unanchored.</param>
    /// <param name="failMessage">On failure, may contain a message stating why the target could not be (un)anchored.</param>
    /// <param name="actualDelay">On success, contains the actual delay that the entity should take to anchor, in seconds.</param>
    /// <param name="anchorable">The AnchorableComponent of the target.</param>
    /// <param name="usingTool">The ToolComponent of the tool.</param>
    /// <returns>true if the target can be (un)anchored, false otherwise.</returns>
    private bool Valid(
        EntityUid uid,
        EntityUid userUid,
        EntityUid usingUid,
        bool anchoring,
        out string? failMessage,
        out float actualDelay,
        AnchorableComponent? anchorable = null,
        ToolComponent? usingTool = null,
        bool checkTool = true) // Arcane-Edit: ToolComponent? usingTool = null) > ToolComponent? usingTool = null, bool checkTool = true)
    {
        actualDelay = 0f;
        failMessage = null;

        if (!Resolve(uid, ref anchorable))
            return false;

        if (checkTool && !Resolve(usingUid, ref usingTool)) // Arcane-Edit: if (!Resolve(usingUid, ref usingTool)) > if (checkTool && !Resolve(usingUid, ref usingTool))
            return false;

        if (anchoring && (anchorable.Flags & AnchorableFlags.Anchorable) == 0x0)
            return false;

        if (!anchoring && (anchorable.Flags & AnchorableFlags.Unanchorable) == 0x0)
            return false;

        BaseAnchoredAttemptEvent attempt =
            anchoring ? new AnchorAttemptEvent(userUid, usingUid) : new UnanchorAttemptEvent(userUid, usingUid);

        // Need to cast the event or it will be raised as BaseAnchoredAttemptEvent.
        if (anchoring)
            RaiseLocalEvent(uid, (AnchorAttemptEvent)attempt);
        else
            RaiseLocalEvent(uid, (UnanchorAttemptEvent)attempt);

        actualDelay = anchorable.Delay + attempt.Delay;

        failMessage = attempt.FailMessage;

        return !attempt.Cancelled;
    }

    public bool CanAnchorAt(Entity<PhysicsComponent?> entity, EntityUid? user = null)
    {
        return CanAnchorAt(entity, Transform(entity).Coordinates, user);
    }

    public bool CanAnchorAt(Entity<PhysicsComponent?> entity, EntityCoordinates coordinates, EntityUid? user = null)
    {
        if (!Resolve(entity, ref entity.Comp))
            return true;

        if (TileFree(coordinates, entity.Comp))
            return true;

        _popup.PopupEntity(Loc.GetString("anchorable-occupied"), entity, user);
        return false;
    }

    /// <summary>
    /// Returns true if no hard anchored entities exist on the coordinate tile that would collide with the provided physics body.
    /// </summary>
    public bool TileFree(EntityCoordinates coordinates, PhysicsComponent anchorBody)
    {
        // Probably ignore CanCollide on the anchoring body?
        var gridUid = _transformSystem.GetGrid(coordinates);

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var tileIndices = _map.TileIndicesFor((gridUid.Value, grid), coordinates);
        return TileFree((gridUid.Value, grid), tileIndices, anchorBody.CollisionLayer, anchorBody.CollisionMask);
    }

    /// <summary>
    /// Returns true if no hard anchored entities match the collision layer or mask specified.
    /// </summary>
    /// <param name="grid"></param>
    public bool TileFree(Entity<MapGridComponent> grid, Vector2i gridIndices, int collisionLayer = 0, int collisionMask = 0)
    {
        var enumerator = _map.GetAnchoredEntities(grid, grid.Comp, gridIndices);

        while (enumerator.MoveNext(out var ent))
        {
            if (!_physicsQuery.TryGetComponent(ent, out var body) ||
                !body.CanCollide ||
                !body.Hard)
            {
                continue;
            }

            if ((body.CollisionMask & collisionLayer) != 0x0 ||
                (body.CollisionLayer & collisionMask) != 0x0)
            {
                return false;
            }
        }

        return true;
    }

    [Obsolete("Use the Entity<MapGridComponent> version")]
    public bool TileFree(MapGridComponent grid, Vector2i gridIndices, int collisionLayer = 0, int collisionMask = 0)
    {
        return TileFree((grid.Owner, grid), gridIndices, collisionLayer, collisionMask);
    }

    /// <summary>
    /// Returns true if any unstackables are also on the corresponding tile.
    /// </summary>
    public bool AnyUnstackable(EntityUid uid, EntityCoordinates location)
    {
        DebugTools.Assert(!Transform(uid).Anchored);

        // If we are unstackable, iterate through any other entities anchored on the current square
        return _tagSystem.HasTag(uid, Unstackable) && AnyUnstackablesAnchoredAt(location);
    }

    public bool AnyUnstackablesAnchoredAt(EntityCoordinates location)
    {
        var gridUid = _transformSystem.GetGrid(location);

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var enumerator = _map.GetAnchoredEntities(gridUid.Value, grid, _map.LocalToTile(gridUid.Value, grid, location));

        while (enumerator.MoveNext(out var entity))
        {
            // If we find another unstackable here, return true.
            if (_tagSystem.HasTag(entity.Value, Unstackable))
                return true;
        }

        return false;
    }

    [Serializable, NetSerializable]
    private sealed partial class TryUnanchorCompletedEvent : SimpleDoAfterEvent
    {
    }

    [Serializable, NetSerializable]
    private sealed partial class TryAnchorCompletedEvent : SimpleDoAfterEvent
    {
    }
}

[Serializable, NetSerializable]
public enum AnchorVisuals : byte
{
    Anchored
}
