namespace Content.Arcane.Shared.MartialArts;

/// <summary>
/// Added to a mob using <see cref="CapoeiraFlowComponent"/>.
/// It lives on the mob because other players never receive the knowledge entity with the martial art.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CapoeiraFlowUserComponent : Component
{
    /// <summary>
    /// How many spins were started, clients play one whenever this changes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Spins;

    /// <summary>
    /// How many somersaults were started, clients play one whenever this changes.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Flips;

    /// <summary>
    /// Client only: the value of <see cref="Spins"/> that was last shown.
    /// </summary>
    [ViewVariables]
    public int ShownSpins;

    /// <summary>
    /// Client only: when the current spin ends.
    /// </summary>
    [ViewVariables]
    public TimeSpan SpinEnd;

    /// <summary>
    /// Client only: the direction the current spin started from.
    /// </summary>
    [ViewVariables]
    public Direction SpinStart;

    /// <summary>
    /// Client only: whether a spin is overriding the sprite's direction.
    /// </summary>
    [ViewVariables]
    public bool Spinning;

    /// <summary>
    /// Client only: the value of <see cref="Flips"/> that was last shown.
    /// </summary>
    [ViewVariables]
    public int ShownFlips;
}
