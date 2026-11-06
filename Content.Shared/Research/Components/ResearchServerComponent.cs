using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

using Content.Arcane.Common.Research; // Arcane

using Robust.Shared.Serialization; // Arcane

namespace Content.Shared.Research.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ResearchServerComponent : Component
{
    /// <summary>
    /// The name of the server
    /// </summary>
    [AutoNetworkedField]
    [DataField("serverName"), ViewVariables(VVAccess.ReadWrite)]
    public string ServerName = "RDSERVER";

    /// <summary>
    /// The amount of points on the server.
    /// </summary>
    [AutoNetworkedField]
    [DataField("points"), ViewVariables(VVAccess.ReadWrite)]
    public int Points;

    // Arcane-Start
    /// <summary>
    /// Multi-point balance for research network economy.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<ResearchPointAmount> PointBalances = new()
    {
        new()
        {
            Type = "General",
            Amount = 0,
        }
    };

    /// <summary>
    /// Network key used for grouping related RnD servers.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public string NetworkId = "ResearchNet";

    /// <summary>
    /// Snapshot of network log entries for connected clients.
    /// </summary>
    [AutoNetworkedField]
    [DataField]
    public List<ResearchLogEntry> Logs = new();
    // Arcane-End

    /// <summary>
    /// A unique numeric id representing the server
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public int Id;

    /// <summary>
    /// Entities connected to the server
    /// </summary>
    /// <remarks>
    /// This is not safe to read clientside
    /// </remarks>
    [ViewVariables(VVAccess.ReadOnly)]
    public List<EntityUid> Clients = new();

    [DataField("nextUpdateTime", customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextUpdateTime = TimeSpan.Zero;

    [DataField("researchConsoleUpdateTime"), ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ResearchConsoleUpdateTime = TimeSpan.FromSeconds(1);
}

/// <summary>
/// Event raised on a server's clients when the point value of the server is changed.
/// </summary>
/// <param name="Server"></param>
/// <param name="Total"></param>
/// <param name="Delta"></param>
[ByRefEvent]
public readonly record struct ResearchServerPointsChangedEvent(EntityUid Server, int Total, int Delta);

// Arcane-Start
[ByRefEvent]
public readonly record struct ResearchServerPointTypeChangedEvent(EntityUid Server, string Type, int Total, int Delta);
// Arcane-End

/* // Arcane-Edit: Use ResearchServerGetPointsPerSecondByTypeEvent
/// <summary>
/// Event raised every second to calculate the amount of points added to the server.
/// </summary>
/// <param name="Server"></param>
/// <param name="Points"></param>
[ByRefEvent]
public record struct ResearchServerGetPointsPerSecondEvent(EntityUid Server, int Points);
*/

// Arcane-Start
/// <summary>
/// Event raised every second to calculate the amount of points added to the server.
/// </summary>
/// <param name="Server"></param>
/// <param name="Points"></param>
[ByRefEvent]
public record struct ResearchServerGetPointsPerSecondByTypeEvent(EntityUid Server, List<ResearchPointAmount> Points);
// Arcane-End

// Arcane-Start
[RegisterComponent]
public sealed partial class ResearchServerControlConsoleComponent : Component;

[RegisterComponent]
public sealed partial class ResearchServerControlStatusComponent : Component
{
    [DataField]
    public bool GenerationEnabled = true;
}

[NetSerializable, Serializable]
public enum ResearchServerControlUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed partial class ToggleServerGenerationMessage : BoundUserInterfaceMessage
{
    public int ServerId;

    public ToggleServerGenerationMessage(int serverId)
    {
        ServerId = serverId;
    }
}

[Serializable, NetSerializable]
public sealed partial class ResearchServerControlBoundInterfaceState : BoundUserInterfaceState
{
    public List<ResearchServerControlEntry> Servers;

    public ResearchServerControlBoundInterfaceState(List<ResearchServerControlEntry> servers)
    {
        Servers = servers;
    }
}

[Serializable, NetSerializable]
public sealed class ResearchServerControlEntry
{
    public int Id;
    public NetEntity ServerEntity;
    public string Name;
    public string NetworkId;
    public bool IsNetworkAuthority;
    public int NetworkAuthorityId;
    public bool GenerationEnabled;
    public int TotalPointsPerSecond;
    public List<ResearchPointAmount> PointGeneration;
    public List<ResearchPointAmount> Balances;
    public int LogCount;

    public ResearchServerControlEntry(int id, NetEntity serverEntity, string name, string networkId, bool isNetworkAuthority, int networkAuthorityId, bool generationEnabled, int totalPointsPerSecond, List<ResearchPointAmount> pointGeneration, List<ResearchPointAmount> balances, int logCount)
    {
        Id = id;
        ServerEntity = serverEntity;
        Name = name;
        NetworkId = networkId;
        IsNetworkAuthority = isNetworkAuthority;
        NetworkAuthorityId = networkAuthorityId;
        GenerationEnabled = generationEnabled;
        TotalPointsPerSecond = totalPointsPerSecond;
        PointGeneration = pointGeneration;
        Balances = balances;
        LogCount = logCount;
    }
}
// Arcane-End
