namespace Content.Client.IconSmoothing;

// Arcane-Edit-Start
/// <summary>
///     Configures sprite smoothing for grid-aligned entities.
/// </summary>
/// <remarks>
///     Use <see cref="Key"/> to identify this entity to neighboring smoothers and
///     <see cref="States"/> to configure its sprite layers.
/// </remarks>
[RegisterComponent]
[Access(typeof(IconSmoothSystem))]
public sealed partial class IconSmoothComponent : Component
{
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// A string whitelist which is checked against an <see cref="ISpriteSmoothState.Mask"/>
    /// If the mask contains this key, then that mask can smooth with this entity.
    /// </summary>
    [DataField(required: true)]
    public string Key { get; private set; }

    /// <summary>
    /// Array of <see cref="ISpriteSmoothState"/> which each apply custom smoothing for individual sprite layers.
    /// </summary>
    [DataField]
    public ISpriteSmoothState[] States { get; private set; } = [];
}
// Arcane-Edit-End
