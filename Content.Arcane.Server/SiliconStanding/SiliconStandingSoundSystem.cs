// Arcane-Start: retain the silicon's footstep sound when it rests or stands
using Content.Arcane.Common.SiliconStanding;
using Content.Arcane.Shared.SiliconStanding;
using Content.Shared.Movement.Components;
using Robust.Server.Audio;

namespace Content.Arcane.Server.SiliconStanding;

public sealed partial class SiliconStandingSoundSystem : EntitySystem
{
    [Dependency] private AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SiliconStandingComponent, BorgRestVisualsChangedEvent>(OnRestChanged);
    }

    private void OnRestChanged(Entity<SiliconStandingComponent> ent, ref BorgRestVisualsChangedEvent args)
    {
        if (TryComp<FootstepModifierComponent>(ent, out var footsteps))
            _audio.PlayPvs(footsteps.FootstepSoundCollection, ent);
    }
}
// Arcane-End
