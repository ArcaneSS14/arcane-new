namespace Content.Arcane.Shared.MartialArts;

/// <summary>
/// Added to a mob using <see cref="CapoeiraParryComponent"/>.
/// It lives on the mob because attackers predict hits without having the martial art's knowledge entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CapoeiraParryWindowComponent : Component
{
    /// <summary>
    /// When the window closes, it also closes after a single attempt.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan End;

    /// <summary>
    /// The chance to deflect the next incoming attack.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Chance;
}
