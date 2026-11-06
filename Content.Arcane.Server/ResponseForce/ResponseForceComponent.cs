using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.ResponseForce;

[RegisterComponent]
public sealed partial class ResponseForceComponent : Component
{
    /// <summary>
    /// A dictionary mapping the component type list to the YAML mapping containing their settings.
    /// </summary>
    [DataField, AlwaysPushInheritance]
    public ComponentRegistry Components { get; private set; } = new();
}

/// <summary>
/// Why a response force call did not happen.
/// </summary>
public enum ResponseForceCallResult : byte
{
    Success,
    UnknownTeam,
    InvalidTeam,
    NotInRound,
    OnCooldown,
    ShuttleLoadFailed,
}

public sealed class ResponseForceHistory
{
    public TimeSpan RoundTime { get; set; }
    public string Event { get; set; } = default!;
    public string WhoCalled { get; set; } = default!;
}
