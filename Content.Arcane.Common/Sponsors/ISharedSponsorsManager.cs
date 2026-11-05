using System.Diagnostics.CodeAnalysis;

namespace Content.Arcane.Common.Sponsors;

/// <summary>
/// Optional sponsor provider. Implement it and register it in IoC to enable sponsor-only content,
/// extra character slots, sponsor OOC colour and priority join.
/// </summary>
/// <remarks>
/// Nothing is registered by default, in which case every sponsor feature degrades to a no-op.
/// </remarks>
public interface ISharedSponsorsManager
{
    public void Initialize();

    // Client
    public List<string> GetClientPrototypes();

    // Server
    public bool TryGetServerPrototypes(NetUserId userId, [NotNullWhen(true)] out List<string>? prototypes);
    public bool TryGetServerOocColor(NetUserId userId, [NotNullWhen(true)] out Color? color);
    public int GetServerExtraCharSlots(NetUserId userId);
    public bool HaveServerPriorityJoin(NetUserId userId);
}
