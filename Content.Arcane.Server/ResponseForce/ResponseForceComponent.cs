using Robust.Shared.Map;
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

public static class ResponseForceState
{
    [ViewVariables]
    public static List<ResponseForceHistory> CalledEvents { get; } = new();

    [ViewVariables]
    public static TimeSpan LastCallTime { get; set; } = TimeSpan.Zero;

    [ViewVariables]
    public static MapId? ShipyardMap { get; set; } = null;

    [ViewVariables]
    public static float ShuttleIndex { get; set; } = 0f;
}

public sealed class ResponseForceHistory
{
    public TimeSpan RoundTime { get; set; }
    public string Event { get; set; } = default!;
    public string WhoCalled { get; set; } = default!;
}
