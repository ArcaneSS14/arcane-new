using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Flash;

public sealed partial class FlashSystem : SharedFlashSystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IOverlayManager _overlayMan = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private FlashOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();
        _overlay = new();
    }

    [SubscribeLocalEvent]
    private void OnApplied(Entity<FlashedStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        if (_player.LocalEntity == args.Target)
        {
            _overlay.Start(); // Arcane-Edit: Reset and capture a fresh frame for each flash.
            _overlayMan.AddOverlay(_overlay);
        }
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<FlashedStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (_player.LocalEntity != args.Target)
            return;

        if (!_statusEffects.HasEffectComp<FlashedStatusEffectComponent>(args.Target))
        {
            _overlay.Reset(); // Arcane-Edit: Release the owned snapshot and stop capture requests.
            _overlayMan.RemoveOverlay(_overlay);
        }
    }

    [SubscribeLocalEvent]
    private void OnPlayerAttached(Entity<FlashedStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerAttachedEvent> args)
    {
        _overlay.Start(captureFrame: false); // Arcane-Edit: An already flashed body has no known last visible frame.
        _overlayMan.AddOverlay(_overlay);
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(Entity<FlashedStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerDetachedEvent> args)
    {
        // Arcane-Edit-Start: A detached player with no new body must also release the snapshot.
        if (_player.LocalEntity is { } player && player != args.AppliedTo &&
            _statusEffects.HasEffectComp<FlashedStatusEffectComponent>(player))
        // Arcane-Edit-End
            return;

        _overlay.Reset(); // Arcane-Edit: Release the owned snapshot and stop capture requests.
        _overlayMan.RemoveOverlay(_overlay);
    }

    // Arcane-Start
    public override void Shutdown()
    {
        _overlayMan.RemoveOverlay(_overlay);
        _overlay.Dispose();
        base.Shutdown();
    }
    // Arcane-End
}
