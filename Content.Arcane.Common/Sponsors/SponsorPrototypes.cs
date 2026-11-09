using Robust.Shared.Player;

namespace Content.Arcane.Common.Sponsors;

/// <summary>
/// Helpers for resolving the sponsor prototypes available to a session.
/// </summary>
public static class SponsorPrototypes
{
    /// <summary>
    /// Gets the sponsor prototypes the given session is allowed to use. Returns an empty array when no
    /// <see cref="ISharedSponsorsManager"/> is registered.
    /// </summary>
    public static string[] Get(ICommonSession? session, IDependencyCollection? collection = null)
    {
        collection ??= IoCManager.Instance;
        if (collection == null || !collection.TryResolveType<ISharedSponsorsManager>(out var sponsors))
            return [];

        if (collection.Resolve<INetManager>().IsClient)
            return sponsors.GetClientPrototypes().ToArray();

        if (session == null ||
            !sponsors.TryGetServerPrototypes(session.UserId, out var prototypes) ||
            prototypes == null)
        {
            return [];
        }

        return prototypes.ToArray();
    }
}
