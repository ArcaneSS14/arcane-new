using Robust.Shared.GameObjects;

namespace Content.Arcane.Server.Atmos;

[RegisterComponent]
public sealed partial class PressureExposureComponent : Component
{
    /// <summary>
    /// Completed seconds of continuous exposure to hazardous low pressure.
    /// </summary>
    [DataField]
    public float Elapsed;
}
