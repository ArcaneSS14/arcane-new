namespace Content.Arcane.Shared.MartialArts;

/// <summary>
/// Goes on a martial art so that a master's strikes open a short window to deflect the next incoming attack: a strike, shove, grab or shot.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(CapoeiraParrySystem))]
public sealed partial class CapoeiraParryComponent : Component
{
    /// <summary>
    /// The level this starts working at, with <see cref="MinChance"/>.
    /// </summary>
    [DataField]
    public int MinLevel = 95;

    /// <summary>
    /// The level that reaches <see cref="MaxChance"/>.
    /// </summary>
    [DataField]
    public int MaxLevel = 100;

    /// <summary>
    /// The chance to deflect an attack at <see cref="MinLevel"/>.
    /// </summary>
    [DataField]
    public float MinChance = 0.1f;

    /// <summary>
    /// The chance to deflect an attack at <see cref="MaxLevel"/>.
    /// </summary>
    [DataField]
    public float MaxChance = 0.5f;

    /// <summary>
    /// How long the window stays open after a strike.
    /// </summary>
    [DataField]
    public TimeSpan WindowTime = TimeSpan.FromSeconds(0.5);
}
