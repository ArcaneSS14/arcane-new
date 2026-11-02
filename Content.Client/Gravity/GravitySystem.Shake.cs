using System.Numerics;
using Content.Shared.Camera;
using Content.Shared.Gravity;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Client.Gravity;

public sealed partial class GravitySystem
{
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedCameraRecoilSystem _sharedCameraRecoil = default!;

    private void InitializeShake()
    {
        SubscribeLocalEvent<GravityShakeComponent, AfterAutoHandleStateEvent>(OnShakeInit); // Arcane-Edit: ComponentInit > AfterAutoHandleStateEvent
    }

    private void OnShakeInit(EntityUid uid, GravityShakeComponent component, ref AfterAutoHandleStateEvent args) // Arcane-Edit: ComponentInit > AfterAutoHandleStateEvent
    {
        // Arcane-Start
        if (component.ArcaneClientSoundHandled)
            return;

        component.ArcaneClientSoundHandled = true;
        if (component.ArcaneSoundIsServerPlayed)
            return;
        // Arcane-End

        var localPlayer = _playerManager.LocalEntity;

        if (!TryComp(localPlayer, out TransformComponent? xform) ||
            xform.GridUid != uid && xform.MapUid != uid)
        {
            return;
        }

        if (TryComp<GravityComponent>(uid, out var gravity)) // Arcane-Edit: first prediction > replicated state
        {
            var audioParams = gravity.GravityShakeSound?.Params ?? AudioParams.Default;
            audioParams = audioParams.AddVolume(-2f);
            _audio.PlayGlobal(gravity.GravityShakeSound, Filter.Local(), true, audioParams);
        }
    }

    protected override void ShakeGrid(EntityUid uid, GravityComponent? gravity = null)
    {
        base.ShakeGrid(uid, gravity);

        if (!Resolve(uid, ref gravity) || !Timing.IsFirstTimePredicted)
            return;

        var localPlayer = _playerManager.LocalEntity;

        if (!TryComp(localPlayer, out TransformComponent? xform))
            return;

        if (xform.GridUid != uid ||
            xform.GridUid == null && xform.MapUid != uid)
        {
            return;
        }

        var kick = new Vector2(_random.NextFloat(), _random.NextFloat()) * GravityKick;
        _sharedCameraRecoil.KickCamera(localPlayer.Value, kick);
    }
}
