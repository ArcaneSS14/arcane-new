// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Player;
using Robust.Shared.Player;
using Content.Trauma.Shared.Kudzu;

namespace Content.Trauma.Client.Kudzu;

public sealed partial class FoliageVisionSystem : EntitySystem
{
    private const int HighDrawDepth = 10;
    private const int LowDrawDepth = -5;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    private bool _enabled;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FoliageIgnoringVisionComponent, LocalPlayerAttachedEvent>((_, _, args) => OnLocalPlayerAttached(args));
        SubscribeLocalEvent<FoliageIgnoringVisionComponent, LocalPlayerDetachedEvent>((_, _, args) => OnLocalPlayerDetached(args));
        SubscribeLocalEvent<FoliageIgnoringVisionComponent, ComponentStartup>((uid, _, args) => OnPlayerComponentStartup(uid, args));
        SubscribeLocalEvent<FoliageIgnoringVisionComponent, ComponentShutdown>((uid, _, args) => OnPlayerComponentShutdown(uid, args));
        SubscribeLocalEvent<HideableFoliageComponent, ComponentStartup>((uid, _, args) => OnKudzuComponentStartup(uid, args));
        SubscribeLocalEvent<HideableFoliageComponent, ComponentShutdown>((uid, _, args) => OnKudzuComponentShutdown(uid, args));
    }

    // Arcane-Start: restore ordinary layering before the local vision owner is torn down.
    public override void Shutdown()
    {
        SetFoliageIgnoringVision(false);
        base.Shutdown();
    }
    // Arcane-End

    // Attaches detaches
    private void OnLocalPlayerAttached(LocalPlayerAttachedEvent args)
    {
        UpdatePlayerFoliageIgnoringVision();
    }

    private void OnLocalPlayerDetached(LocalPlayerDetachedEvent args)
    {
        SetFoliageIgnoringVision(false); // Arcane-Edit: UpdatePlayerFoliageIgnoringVision() > SetFoliageIgnoringVision(false); detach can still expose the old LocalEntity
    }

    // Player Startup/Shutdown
    private void OnPlayerComponentStartup(EntityUid uid, ComponentStartup args)
    {
        if (uid == _player.LocalEntity)
            UpdatePlayerFoliageIgnoringVision();
    }

    private void OnPlayerComponentShutdown(EntityUid uid, ComponentShutdown args)
    {
        if (uid == _player.LocalEntity)
            SetFoliageIgnoringVision(false); // Arcane-Edit: UpdatePlayerFoliageIgnoringVision() > SetFoliageIgnoringVision(false); shutdown still has the component
    }

    // Kudzu Startup/Shutdown
    private void OnKudzuComponentStartup(EntityUid uid, ComponentStartup args)
    {
        UpdateFoliageDrawDepth(uid);
    }

    private void OnKudzuComponentShutdown(EntityUid uid, ComponentShutdown args)
    {
        UpdateFoliageDrawDepth(uid, forceVisible: true); // Arcane-Edit: UpdateFoliageDrawDepth(uid) > UpdateFoliageDrawDepth(uid, forceVisible: true)
    }

    private void RefreshEveryPieceOfFoliage()
    {
        var query = EntityQueryEnumerator<HideableFoliageComponent, SpriteComponent>();

        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            UpdateFoliageDrawDepth(uid, sprite);
        }
    }

    private void UpdateFoliageDrawDepth(EntityUid uid, SpriteComponent? sprite = null, bool forceVisible = false) // Arcane-Edit: (EntityUid uid, SpriteComponent? sprite = null) > add bool forceVisible = false
    {
        if (!Resolve(uid, ref sprite, false))
            return;

        var drawDepth = _enabled && !forceVisible // Arcane-Edit: _enabled > _enabled && !forceVisible
            ? DrawDepth.Default + LowDrawDepth
            : DrawDepth.Default + HighDrawDepth;
        _sprite.SetDrawDepth((uid, sprite), drawDepth);
    }

    private void UpdatePlayerFoliageIgnoringVision()
    {
        // var previousEnabled = _enabled; // Arcane-Edit: local change tracking > SetFoliageIgnoringVision
        var attached = _player.LocalEntity;
        var shouldHaveFoliageIgnoringVision = attached != null && HasComp<FoliageIgnoringVisionComponent>(attached.Value);
        SetFoliageIgnoringVision(shouldHaveFoliageIgnoringVision); // Arcane-Edit: inline enabled-state update and refresh > SetFoliageIgnoringVision(shouldHaveFoliageIgnoringVision)
    }

    // Arcane-Start
    private void SetFoliageIgnoringVision(bool enabled)
    {
        if (_enabled == enabled)
            return;
        _enabled = enabled;
        RefreshEveryPieceOfFoliage();
    }
    // Arcane-End
}
