using Content.Client.Lock.Visualizers;
using Content.Client.Storage.Visualizers;
using Content.Client.Tools.Visualizers;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using Content.Shared.Labels;
using Content.Shared.Lock;
using Content.Shared.Storage;
using Content.Shared.Tools.Components;
using Robust.Shared.Map;
using Robust.Client.Graphics;

namespace Content.Arcane.Client.Storage;

/// <summary>
/// Animates presentation after the ordinary visualizers have resolved the authoritative appearance.
/// Storage contents, access checks and collisions remain owned by EntityStorage.
/// </summary>
public sealed partial class StorageAnimationSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private AppearanceSystem _appearance = default!;

    private readonly HashSet<EntityUid> _active = [];
    private readonly List<EntityUid> _finished = [];

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(AppearanceSystem));
        SubscribeLocalEvent<StorageAnimationVisualsComponent, AppearanceChangeEvent>(OnAppearance,
            after: [typeof(EntityStorageVisualizerSystem), typeof(LockVisualizerSystem),
                typeof(GenericVisualizerSystem), typeof(WeldableVisualizerSystem)]);
        SubscribeLocalEvent<StorageAnimationVisualsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<StorageAnimationVisualsComponent, EntParentChangedMessage>(OnParentChanged);
    }

    private void OnAppearance(EntityUid uid, StorageAnimationVisualsComponent comp, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null || !_appearance.TryGetData<bool>(uid, StorageVisuals.Open, out var open, args.Component))
            return;

        if (!TryComp<EntityStorageVisualsComponent>(uid, out var visuals)
            || !Compatible(uid, comp, args.Sprite, visuals))
        {
            Stop(uid, comp, false);
            comp.AppearanceInitialized = false;
            return;
        }

        if (!comp.AppearanceInitialized)
        {
            comp.AppearanceInitialized = true;
            comp.Open = open;
            comp.Phase = open ? 1f : 0f;
            return;
        }

        if (comp.Open != open)
        {
            comp.Open = open;
            comp.Active = true;
            _active.Add(uid);
        }

        if (!comp.Active)
            return;

        comp.ClosedState = visuals.StateDoorClosed!;
        comp.OpenState = visuals.StateDoorOpen!;
        BindDoor(uid, comp, args.Sprite, args.Component);
        Draw(uid, comp, args.Sprite);
    }

    private bool Compatible(EntityUid uid, StorageAnimationVisualsComponent comp, SpriteComponent sprite,
        EntityStorageVisualsComponent visuals)
    {
        if (sprite.BaseRSI == null || comp.Duration <= TimeSpan.Zero
            || visuals.StateDoorOpen == null || visuals.StateDoorClosed == null
            || !comp.OpenStates.Contains(visuals.StateDoorOpen)
            || !comp.Sprites.Contains(sprite.BaseRSI.Path.RelativeTo(new ResPath("/Textures")))
            || !sprite.BaseRSI.TryGetState(visuals.StateDoorOpen, out _)
            || !sprite.BaseRSI.TryGetState(visuals.StateDoorClosed, out _))
            return false;

        comp.ResolvedHinge = comp.Hinges.GetValueOrDefault(sprite.BaseRSI.Path.RelativeTo(new ResPath("/Textures")), comp.Hinge);
        comp.OverlayCutoff = comp.OverlayCutoffs.GetValueOrDefault(sprite.BaseRSI.Path.RelativeTo(new ResPath("/Textures")));
        comp.BodyCutoff = comp.BodyCutoffs.GetValueOrDefault(sprite.BaseRSI.Path.RelativeTo(new ResPath("/Textures")));
        comp.DoubleDoors = comp.DoubleDoorStates.Contains(visuals.StateDoorOpen);
        if (comp.DoubleDoors)
            comp.ResolvedHinge = comp.LeftHinge;
        return _sprite.LayerMapTryGet((uid, sprite), StorageVisualLayers.Door, out comp.DoorLayer, false);
    }

    private void BindDoor(EntityUid uid, StorageAnimationVisualsComponent comp, SpriteComponent source,
        AppearanceComponent appearance)
    {
        ClearFixedParts(uid, comp);
        if (!comp.DoubleDoors)
            ClearRightDoor(comp);
        if (comp.Door is not { } door || Deleted(door))
        {
            door = Spawn(null, new EntityCoordinates(uid, Vector2.Zero));
            comp.Door = door;
        }

        var sprite = EnsureComp<SpriteComponent>(door);
        _sprite.CopySprite((uid, source), (door, sprite));
        comp.Layers.Clear();

        SpriteComponent? fixedSprite = null;
        if (comp.OverlayCutoff > 0 || comp.BodyCutoff > 0)
        {
            if (comp.FixedOverlay is not { } existing || Deleted(existing))
                comp.FixedOverlay = Spawn(null, new EntityCoordinates(uid, Vector2.Zero));
            fixedSprite = EnsureComp<SpriteComponent>(comp.FixedOverlay.Value);
            _sprite.CopySprite((uid, source), (comp.FixedOverlay.Value, fixedSprite));
            _sprite.SetDrawDepth((comp.FixedOverlay.Value, fixedSprite), source.DrawDepth + 1);
        }

        for (var i = 0; _sprite.TryGetLayer((door, sprite), i, out var layer, false); i++)
        {
            var moving = i == comp.DoorLayer;
            var visible = moving || layer.Visible;
            var backOnly = false;

            if (fixedSprite != null)
            {
                _sprite.LayerSetVisible((comp.FixedOverlay!.Value, fixedSprite), i, false);
                var frontOverlay = _sprite.LayerMapTryGet((uid, source), "lid_overlay", out var front, false) && i == front;
                backOnly = _sprite.LayerMapTryGet((uid, source), "open_overlay", out var back, false) && i == back;
                if (frontOverlay || backOnly)
                {
                    moving = true;
                    visible = true;
                    layer.ActualRsi!.TryGetState(frontOverlay ? "overlay-closed" : "overlay-open", out var overlayState);
                    var frame = overlayState!.Icons[0][0];
                    var cutoff = comp.OverlayCutoff;
                    if (frontOverlay)
                    {
                        _sprite.LayerSetTexture((comp.FixedOverlay.Value, fixedSprite), i, Slice(frame, cutoff, frame.Height));
                        _sprite.LayerSetOffset((comp.FixedOverlay.Value, fixedSprite), i,
                            layer.Offset + new Vector2(0, -cutoff / 64f));
                        _sprite.LayerSetVisible((comp.FixedOverlay.Value, fixedSprite), i, true);
                    }
                    _sprite.LayerSetTexture(layer, Slice(frame, 0, cutoff));
                    _sprite.LayerSetOffset(layer, layer.Offset + new Vector2(0, (frame.Height - cutoff) / 64f));
                }

                if (comp.BodyCutoff > 0 && IsLayer(uid, source, i, StorageVisualLayers.Base))
                {
                    source.BaseRSI!.TryGetState(comp.Open ? comp.OpenState : comp.ClosedState, out var bodyState);
                    var frame = bodyState!.Icons[0][0];
                    _sprite.LayerSetTexture((comp.FixedOverlay!.Value, fixedSprite), i, Slice(frame, comp.BodyCutoff, frame.Height));
                    _sprite.LayerSetOffset((comp.FixedOverlay.Value, fixedSprite), i,
                        layer.Offset + new Vector2(0, -comp.BodyCutoff / 64f));
                    _sprite.LayerSetVisible((comp.FixedOverlay.Value, fixedSprite), i, true);
                    comp.HiddenBaseLayer = i;
                    _sprite.LayerSetVisible((uid, source), i, false);
                }
            }

            if (i == comp.DoorLayer && comp.BodyCutoff > 0)
            {
                source.BaseRSI!.TryGetState(comp.ClosedState, out var frontState);
                source.BaseRSI.TryGetState(comp.OpenState, out var backState);
                comp.FrontTexture = Slice(frontState!.Icons[0][0], 0, comp.BodyCutoff);
                comp.BackTexture = Slice(backState!.Icons[0][0], 0, comp.BodyCutoff);
                _sprite.LayerSetOffset(layer, layer.Offset + new Vector2(0, (frontState.Size.Y - comp.BodyCutoff) / 64f));
            }

            if (comp.OverlayCutoff == 0 && IsLayer(uid, source, i, LockVisualLayers.Lock))
            {
                moving = true;
                visible = true;
                // The normal visualizer has already hidden this layer for an open target.
                if (TryComp<LockVisualsComponent>(uid, out var lockVisuals)
                    && _appearance.TryGetData<bool>(uid, LockVisuals.Locked, out var locked, appearance))
                {
                    var state = locked ? lockVisuals.StateLocked : lockVisuals.StateUnlocked;
                    visible = state != null && layer.ActualRsi != null && layer.ActualRsi.TryGetState(state, out _);
                    if (visible)
                        _sprite.LayerSetRsiState(layer, state!);
                }
            }

            if (!comp.Lid && IsLayer(uid, source, i, PaperLabelVisuals.Layer))
            {
                moving = true;
                visible = _appearance.TryGetData<bool>(uid, PaperLabelVisuals.HasLabel, out var label, appearance) && label;
            }

            if (IsLayer(uid, source, i, WeldableLayers.BaseWelded))
                moving = true;

            _sprite.LayerSetVisible(layer, moving && visible);
            if (!moving)
                continue;

            comp.Layers.Add(new StorageAnimationLayer(i, layer.Offset, layer.Rotation, layer.Scale, visible, backOnly));
            _sprite.LayerSetVisible((uid, source), i, false);
        }

        if (comp.DoubleDoors)
            BindDoubleDoors(uid, comp, source, (door, sprite));
    }

    private void BindDoubleDoors(EntityUid uid, StorageAnimationVisualsComponent comp, SpriteComponent source,
        Entity<SpriteComponent> left)
    {
        if (comp.RightDoor is not { } right || Deleted(right))
        {
            right = Spawn(null, new EntityCoordinates(uid, Vector2.Zero));
            comp.RightDoor = right;
        }

        var sprite = EnsureComp<SpriteComponent>(right);
        _sprite.CopySprite((left.Owner, left.Comp), (right, sprite));
        source.BaseRSI!.TryGetState(comp.ClosedState, out var closed);
        source.BaseRSI.TryGetState(comp.OpenState, out var open);
        comp.FrontTexture = Half(closed!.Icons[0][0], false);
        comp.BackTexture = Half(open!.Icons[0][0], false);
        comp.RightFrontTexture = Half(closed.Icons[0][0], true);
        comp.RightBackTexture = Half(open.Icons[0][0], true);
        comp.RightLayers.Clear();

        for (var i = 0; i < comp.Layers.Count; i++)
        {
            var layer = comp.Layers[i];
            if (layer.Index == comp.DoorLayer)
            {
                var offset = new Vector2(closed.Size.X / 128f, 0);
                comp.Layers[i] = layer with { Offset = layer.Offset - offset };
                comp.RightLayers.Add(layer with { Offset = layer.Offset + offset });
                continue;
            }

            // The existing lock and weld artwork lies on the right leaf. Keep the paper
            // label whole on the left leaf, rather than duplicating or tearing it at the seam.
            var onRight = IsLayer(uid, source, layer.Index, LockVisualLayers.Lock)
                          || IsLayer(uid, source, layer.Index, WeldableLayers.BaseWelded);
            comp.Layers[i] = layer with { Visible = layer.Visible && !onRight };
            comp.RightLayers.Add(layer with { Visible = layer.Visible && onRight });
        }
    }

    private static AtlasTexture Half(AtlasTexture frame, bool right)
    {
        var region = frame.SubRegion;
        var middle = region.Left + frame.Width / 2f;
        return new AtlasTexture(frame.SourceTexture,
            new UIBox2(right ? middle : region.Left, region.Top, right ? region.Right : middle, region.Bottom));
    }

    private static AtlasTexture Slice(AtlasTexture frame, int top, int bottom)
    {
        var region = frame.SubRegion;
        return new AtlasTexture(frame.SourceTexture,
            new UIBox2(region.Left, region.Top + top, region.Right, region.Top + bottom));
    }

    private bool IsLayer(EntityUid uid, SpriteComponent sprite, int index, Enum key)
    {
        return _sprite.LayerMapTryGet((uid, sprite), key, out var mapped, false) && index == mapped;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        _finished.Clear();
        foreach (var uid in _active)
        {
            if (!TryComp<StorageAnimationVisualsComponent>(uid, out var comp))
            {
                _finished.Add(uid);
                continue;
            }

            if (!TryComp<SpriteComponent>(uid, out var sprite)
                || (MetaData(uid).Flags & MetaDataFlags.Detached) != 0)
            {
                comp.AppearanceInitialized = false;
                _finished.Add(uid);
                continue;
            }

            if (MetaData(uid).EntityPaused)
                continue;

            // Presentation time must never rewind with prediction or server clock corrections.
            // Reversals start at the last rendered pose, even when a state arrives between frames.
            var step = MathF.Max(0f, frameTime) / (float) comp.Duration.TotalSeconds;
            comp.Phase = Math.Clamp(comp.Phase + (comp.Open ? step : -step), 0f, 1f);
            if (comp.Phase == (comp.Open ? 1f : 0f))
            {
                _finished.Add(uid);
                continue;
            }

            Draw(uid, comp, sprite);
        }

        foreach (var uid in _finished)
        {
            _active.Remove(uid);
            if (TryComp<StorageAnimationVisualsComponent>(uid, out var comp))
                Stop(uid, comp, true);
        }
    }

    private void Draw(EntityUid uid, StorageAnimationVisualsComponent comp, SpriteComponent source)
    {
        if (comp.Door is { } left)
            DrawDoor(comp, source, left, comp.Layers, comp.ResolvedHinge, false, comp.FrontTexture, comp.BackTexture);
        if (comp.RightDoor is { } right)
            DrawDoor(comp, source, right, comp.RightLayers, comp.RightHinge, true, comp.RightFrontTexture, comp.RightBackTexture);

        if (comp.FixedOverlay is { } fixedOverlay)
        {
            _sprite.SetColor(fixedOverlay, source.Color);
            _sprite.SetVisible(fixedOverlay, source.Visible && !source.ContainerOccluded);
            _sprite.SetOffset(fixedOverlay, source.Offset);
            _sprite.SetRotation(fixedOverlay, source.Rotation);
            _sprite.SetScale(fixedOverlay, source.Scale);
        }
    }

    private void DrawDoor(StorageAnimationVisualsComponent comp, SpriteComponent source, EntityUid door,
        List<StorageAnimationLayer> layers, Vector2 hinge, bool mirror, Texture? frontTexture, Texture? backTexture)
    {
        if (!TryComp<SpriteComponent>(door, out var sprite))
            return;

        var angle = comp.Lid ? comp.Phase * MathF.PI : comp.Phase * MathHelper.DegreesToRadians(comp.OpenAngle);
        var back = angle >= MathF.PI / 2f;
        var motion = DoorTransform(comp, angle, back);
        if (mirror)
            motion.M12 = -motion.M12;
        var owner = Matrix3x2.CreateScale(source.Scale)
                    * Matrix3x2.CreateRotation((float) source.Rotation.Theta)
                    * Matrix3x2.CreateTranslation(source.Offset);
        var transform = Matrix3x2.CreateTranslation(-hinge) * motion
                        * Matrix3x2.CreateTranslation(hinge) * owner;

        // A sprite and its layers each expose rotation/scale, but no shear setter.
        // Factor the affine transform into those two supported transforms.
        Decompose(transform, out var outer, out var scale, out var inner);
        _sprite.SetOffset((door, sprite), transform.Translation);
        _sprite.SetRotation((door, sprite), new Angle(outer));
        _sprite.SetScale((door, sprite), scale);
        _sprite.SetColor((door, sprite), source.Color);
        _sprite.SetVisible((door, sprite), source.Visible && !source.ContainerOccluded);
        sprite.NoRotation = source.NoRotation;
        // A separate sprite at the body's depth can sort behind it as its bounds move.
        _sprite.SetDrawDepth((door, sprite), comp.Lid ? source.DrawDepth + 1 : back ? (int) DrawDepth.BelowMobs : (int) DrawDepth.OverMobs);

        foreach (var layer in layers)
        {
            _sprite.LayerSetOffset((door, sprite), layer.Index, Vector2.Transform(layer.Offset, Matrix3x2.CreateRotation(inner)));
            _sprite.LayerSetRotation((door, sprite), layer.Index, layer.Rotation + new Angle(inner));
            _sprite.LayerSetScale((door, sprite), layer.Index, layer.Scale);
            _sprite.LayerSetVisible((door, sprite), layer.Index,
                layer.Index == comp.DoorLayer || layer.Visible && (layer.BackOnly ? back : !back));
        }

        if (frontTexture != null)
            _sprite.LayerSetTexture((door, sprite), comp.DoorLayer, back ? backTexture : frontTexture);
        else
            _sprite.LayerSetRsiState((door, sprite), comp.DoorLayer, back ? comp.OpenState : comp.ClosedState);
    }

    private static Matrix3x2 DoorTransform(StorageAnimationVisualsComponent comp, float angle, bool back)
    {
        // Keep the edge-on pose invertible; the sprite system rejects nearly-zero scales.
        if (comp.Lid)
            return Matrix3x2.CreateScale(1f, MathF.Max(0.02f, MathF.Abs(MathF.Cos(angle))));

        var cosine = MathF.Cos(angle);
        var shear = -MathF.Sin(angle) * comp.Squish;
        if (!back)
            return new Matrix3x2(MathF.Max(0.02f, cosine), shear, 0, 1, 0, 0);

        // The open artwork already depicts the inside of the door at the final angle.
        // Undo that projection before applying the current one, so the last pose is exact.
        var end = MathHelper.DegreesToRadians(comp.OpenAngle);
        var endCosine = MathF.Cos(end);
        return new Matrix3x2(MathF.Max(0.02f, cosine / endCosine),
            (shear + MathF.Sin(end) * comp.Squish) / endCosine, 0, 1, 0, 0);
    }

    private static void Decompose(Matrix3x2 matrix, out float outer, out Vector2 scale, out float inner)
    {
        var aa = matrix.M11 * matrix.M11 + matrix.M12 * matrix.M12;
        var ab = matrix.M11 * matrix.M21 + matrix.M12 * matrix.M22;
        var bb = matrix.M21 * matrix.M21 + matrix.M22 * matrix.M22;
        var right = 0.5f * MathF.Atan2(2f * ab, aa - bb);
        var cosine = MathF.Cos(right);
        var sine = MathF.Sin(right);
        var x = matrix.M11 * cosine + matrix.M21 * sine;
        var y = matrix.M12 * cosine + matrix.M22 * sine;
        var first = MathF.Sqrt(x * x + y * y);
        var determinant = matrix.M11 * matrix.M22 - matrix.M12 * matrix.M21;
        outer = MathF.Atan2(y, x);
        inner = -right;
        scale = new Vector2(first, determinant / first);
    }

    private void Stop(EntityUid uid, StorageAnimationVisualsComponent comp, bool redraw)
    {
        comp.Active = false;
        _active.Remove(uid);
        if (comp.Door is { } door && !Deleted(door))
        {
            _sprite.SetVisible(door, false);
            QueueDel(door);
        }
        comp.Door = null;
        ClearRightDoor(comp);
        ClearFixedParts(uid, comp);
        comp.Layers.Clear();
        if (redraw && !TerminatingOrDeleted(uid) && TryComp<AppearanceComponent>(uid, out var appearance)
            && TryComp<SpriteComponent>(uid, out var sprite))
            _appearance.OnChangeData(uid, sprite, appearance);
    }

    private void ClearFixedParts(EntityUid uid, StorageAnimationVisualsComponent comp)
    {
        if (comp.FixedOverlay is { } fixedOverlay && !Deleted(fixedOverlay))
        {
            _sprite.SetVisible(fixedOverlay, false);
            QueueDel(fixedOverlay);
        }
        comp.FixedOverlay = null;
        if (comp.HiddenBaseLayer is { } baseLayer && !TerminatingOrDeleted(uid))
            _sprite.LayerSetVisible(uid, baseLayer, true);
        comp.HiddenBaseLayer = null;
        comp.FrontTexture = null;
        comp.BackTexture = null;
    }

    private void ClearRightDoor(StorageAnimationVisualsComponent comp)
    {
        if (comp.RightDoor is { } right && !Deleted(right))
        {
            _sprite.SetVisible(right, false);
            QueueDel(right);
        }
        comp.RightDoor = null;
        comp.RightFrontTexture = null;
        comp.RightBackTexture = null;
        comp.RightLayers.Clear();
    }

    private void OnParentChanged(EntityUid uid, StorageAnimationVisualsComponent comp, ref EntParentChangedMessage args)
    {
        // Re-entry into PVS and transfer into another container/map must start at the observed state.
        comp.AppearanceInitialized = false;
        Stop(uid, comp, true);
        comp.AppearanceInitialized = false;
    }

    private void OnShutdown(EntityUid uid, StorageAnimationVisualsComponent comp, ComponentShutdown args)
    {
        comp.AppearanceInitialized = false;
        Stop(uid, comp, true);
    }
}
