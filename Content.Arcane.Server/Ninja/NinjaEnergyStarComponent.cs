using Robust.Shared.Timing;

namespace Content.Arcane.Server.Ninja;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class NinjaEnergyStarComponent : Component
{
    [AutoPausedField]
    public TimeSpan? NextPulse;

    public bool Hit;
}
