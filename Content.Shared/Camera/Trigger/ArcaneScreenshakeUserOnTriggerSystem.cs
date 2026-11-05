// Arcane-Start
using Content.Shared.Trigger;

namespace Content.Shared.Camera.Trigger;

public sealed partial class ArcaneScreenshakeUserOnTriggerSystem : XOnTriggerSystem<ArcaneScreenshakeUserOnTriggerComponent>
{
    [Dependency] private ArcaneScreenshakeSystem _screenshake = default!;

    protected override void OnTrigger(Entity<ArcaneScreenshakeUserOnTriggerComponent> entity, EntityUid target, ref TriggerEvent args)
    {
        if (args.User is not { } user)
            return;

        _screenshake.Screenshake(user, entity.Comp.Translation, entity.Comp.Rotation);
        args.Handled = true;
    }
}
// Arcane-End
