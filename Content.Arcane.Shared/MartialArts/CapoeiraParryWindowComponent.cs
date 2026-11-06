namespace Content.Arcane.Shared.MartialArts;

/// <summary>
/// Added to a mob using <see cref="CapoeiraParryComponent"/>.
/// It lives on the mob because attackers predict hits without having the martial art's knowledge entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CapoeiraParryWindowComponent : Component
{
    /// <summary>
    /// One attempt per strike that landed, oldest first.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<CapoeiraParryAttempt> Attempts = new();
}

/// <summary>
/// A single chance to deflect an incoming attack.
/// </summary>
[DataRecord, Serializable, NetSerializable]
public partial record struct CapoeiraParryAttempt
{
    /// <summary>
    /// When this attempt runs out.
    /// </summary>
    public TimeSpan End;

    /// <summary>
    /// The chance to deflect the attack.
    /// </summary>
    public float Chance;

    public CapoeiraParryAttempt()
    {
    }
}
