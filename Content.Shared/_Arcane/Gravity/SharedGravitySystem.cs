using Content.Arcane.Common.Camera;
using Content.Shared.Camera;
using Content.Shared.GameTicking;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared.Gravity;

public abstract partial class SharedGravitySystem
{
    [Dependency] private GameTicker _arcaneTicker = default!;
    [Dependency] private SharedAudioSystem _arcaneAudio = default!;
    [Dependency] private INetManager _arcaneNetManager = default!;

    private static readonly ArcaneScreenshakeParameters ArcaneGridShake = new(0.8f, 0.04f, 0.015f);

    private bool TryArcaneScreenshakeGrid(EntityUid uid, GravityComponent gravity)
    {
        if (Timing.CurTime - _arcaneTicker.RoundStartTimeSpan < TimeSpan.FromSeconds(30))
            return false;

        var filter = Filter.BroadcastGrid(uid);
        ArcaneScreenshakeRequest.Raise(EntityManager, filter, ArcaneGridShake, null);
        if (_arcaneNetManager.IsServer)
        {
            var audioParams = gravity.GravityShakeSound?.Params ?? AudioParams.Default;
            _arcaneAudio.PlayGlobal(gravity.GravityShakeSound, filter, true, audioParams.AddVolume(-2f));
        }
        return true;
    }
}
