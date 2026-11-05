// Arcane-Start
using Content.Client.UserInterface.Motion;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Systems.Storage.Controls;

public sealed partial class ItemGridPiece
{
    private readonly IConfigurationManager _motionConfiguration = IoCManager.Resolve<IConfigurationManager>();
    private UiMotionTransition? _motion;
    private UiMotionGhost? _exitGhost;
    private float MotionOpacity => UiMotionSettings.IsEnabled(_motionConfiguration, UiMotionSettings.Slots)
        ? _motion?.Opacity ?? 1 : 1;

    public void AnimateAppearance()
    {
        _exitGhost?.Orphan();
        _exitGhost = null;
        if (_storageController.IsDragging || !UiMotionSettings.IsEnabled(_motionConfiguration, UiMotionSettings.Slots))
            return;
        _motion ??= new UiMotionTransition(_motionConfiguration);
        _motion.Start(new UiMotionProfile(
            TimeSpan.FromMilliseconds(UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.SlotEnterMs, 0, 1000)),
            TimeSpan.Zero, 0, false), true, 0, 0);
    }

    public void AnimateRemoval()
    {
        if (_storageController.IsDragging || !IsInsideTree || !_entityManager.EntityExists(Entity)
            || !UiMotionSettings.IsEnabled(_motionConfiguration, UiMotionSettings.Slots)
            || !_entityManager.TryGetComponent<Content.Shared.Item.ItemComponent>(Entity, out var item))
            return;
        var duration = UiMotionSettings.Read(_motionConfiguration, UiMotionSettings.SlotExitMs, 0, 1000);
        if (duration <= 0)
            return;
        // Storage may override the inventory sprite. Keep that representation in the exit layer.
        Robust.Client.UserInterface.Control icon;
        if (item.StoredSprite is { } stored)
            icon = new TextureRect { Texture = _entityManager.System<Robust.Client.GameObjects.SpriteSystem>().Frame0(stored),
                Stretch = TextureRect.StretchMode.KeepAspectCentered };
        else
        {
            var sprite = new SpriteView { Scale = new System.Numerics.Vector2(2), OverrideDirection = Direction.South };
            sprite.SetEntity(Entity);
            icon = sprite;
        }
        _exitGhost?.Orphan();
        _exitGhost = new UiMotionGhost(icon, UiMotionSettings.Slots, duration, GlobalPosition, Size, Entity);
        Root!.ModalRoot.AddChild(_exitGhost);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _motion?.Advance(args.DeltaSeconds);
        if (!UiMotionSettings.IsEnabled(_motionConfiguration, UiMotionSettings.Slots))
            _motion?.Finish();
    }
}
// Arcane-End
