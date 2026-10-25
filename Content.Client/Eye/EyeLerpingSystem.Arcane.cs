// Arcane-Start
using Content.Shared.Camera;

namespace Content.Client.Eye;

public sealed partial class EyeLerpingSystem
{
    private Angle GetArcaneScreenshakeRotation(EntityUid uid)
    {
        var ev = new ArcaneGetEyeRotationEvent();
        RaiseLocalEvent(uid, ref ev);
        return ev.Rotation;
    }
}
// Arcane-End
