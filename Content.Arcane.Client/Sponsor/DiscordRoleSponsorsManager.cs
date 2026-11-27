using System.Diagnostics.CodeAnalysis;
using Content.Arcane.Client.DiscordRoles;
using Content.Arcane.Common.Sponsors;
using Content.Arcane.Shared.Sponsor;
using Robust.Client.Player;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Client.Sponsor;

/// <summary>
/// The client side of Discord role sponsorship: which sponsor-only markings and species the local player may pick.
/// </summary>
public sealed partial class DiscordRoleSponsorsManager : ISharedSponsorsManager
{
    [Dependency] private DiscordRoleManager _roles = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    public void Initialize()
    {
    }

    public List<string> GetClientPrototypes()
    {
        if (_player.LocalSession is not { } session)
            return [];

        return SponsorPrototypeList.For(_prototypes, role => _roles.HasRole(session, role));
    }

    public bool TryGetServerPrototypes(NetUserId userId, [NotNullWhen(true)] out List<string>? prototypes)
    {
        prototypes = null;
        return false;
    }

    public bool TryGetServerOocColor(NetUserId userId, [NotNullWhen(true)] out Color? color)
    {
        color = null;
        return false;
    }

    public int GetServerExtraCharSlots(NetUserId userId)
    {
        return 0;
    }

    public bool HaveServerPriorityJoin(NetUserId userId)
    {
        return false;
    }
}
