using System.Numerics;
using Content.Arcane.Shared.Polls;
using Content.Goobstation.Client.Polls;
using Content.Shared.Interaction;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Arcane.Client.Polls;

public sealed partial class PollBoothSystem : EntitySystem
{
    [Dependency] private IUserInterfaceManager _ui = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private PollSeenSystem Polls => EntityManager.System<PollSeenSystem>();

    private static readonly SpriteSpecifier AlertSprite =
        new SpriteSpecifier.Rsi(new ResPath("Structures/Storage/closet.rsi"), "cardboard_special");


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PollBoothComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<PollBoothComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PollBoothComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<PollBoothComponent, MoveEvent>(OnMove);
        Polls.OnUnseenChanged += RefreshAll;
    }

    public override void Shutdown()
    {
        Polls.OnUnseenChanged -= RefreshAll;
        base.Shutdown();
    }

    private void OnActivate(Entity<PollBoothComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled || !args.Complex)
            return;

        _ui.GetUIController<PollUIController>().OpenWindow();
        args.Handled = true;
    }

    private void OnStartup(Entity<PollBoothComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (_sprite.LayerMapTryGet((ent.Owner, sprite), PollBoothLayer.Alert, out _, false))
            return;

        var layer = _sprite.AddLayer((ent.Owner, sprite), AlertSprite);
        _sprite.LayerMapSet((ent.Owner, sprite), PollBoothLayer.Alert, layer);
        sprite.LayerSetShader(layer, "unshaded");
        _sprite.LayerSetVisible((ent.Owner, sprite), layer, Polls.HasUnseenPolls);
        UpdateAlertTransform((ent.Owner, sprite), layer);

        Polls.RequestActivePolls();
    }

    private void OnShutdown(Entity<PollBoothComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((ent.Owner, sprite), PollBoothLayer.Alert, out var layer, false))
            return;

        _sprite.RemoveLayer((ent.Owner, sprite), layer);
    }

    private void OnMove(Entity<PollBoothComponent> ent, ref MoveEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        if (!_sprite.LayerMapTryGet((ent.Owner, sprite), PollBoothLayer.Alert, out var layer, false))
            return;

        UpdateAlertTransform((ent.Owner, sprite), layer);
    }

    private void UpdateAlertTransform(Entity<SpriteComponent?> sprite, int layer)
    {
        var rot = Transform(sprite.Owner).LocalRotation;
        _sprite.LayerSetRotation(sprite, layer, -rot);
        _sprite.LayerSetOffset(sprite, layer, (-rot).RotateVec(new Vector2(0f, 1f)));
    }

    private void RefreshAll()
    {
        var hasUnseen = Polls.HasUnseenPolls;
        var query = EntityQueryEnumerator<PollBoothComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            if (_sprite.LayerMapTryGet((uid, sprite), PollBoothLayer.Alert, out var layer, false))
                _sprite.LayerSetVisible((uid, sprite), layer, hasUnseen);
        }
    }
}

public enum PollBoothLayer : byte
{
    Alert,
}
