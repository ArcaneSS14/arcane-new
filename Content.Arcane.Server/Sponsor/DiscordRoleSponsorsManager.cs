using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Content.Arcane.Common.Sponsors;
using Content.Arcane.Server.DiscordRoles;
using Content.Arcane.Shared.DiscordRoles;
using Content.Arcane.Shared.Sponsor;
using Robust.Shared.Network;

namespace Content.Arcane.Server.Sponsor;

/// <summary>
/// Sponsorship from Discord roles: the highest sponsor role picks the OOC colour, and sponsor roles join a
/// full server first. Sponsor gear is gated by role effects on the prototypes, not by a prototype list.
/// </summary>
public sealed partial class DiscordRoleSponsorsManager : ISharedSponsorsManager
{
    [Dependency] private DiscordRoleManager _roles = default!;

    private static readonly DiscordRole[] PriorityJoinRoles =
    [
        DiscordRole.SponsorTier1,
        DiscordRole.SponsorTier2,
        DiscordRole.AdminBenefit,
    ];

    public void Initialize()
    {
    }

    public List<string> GetClientPrototypes()
    {
        return [];
    }

    public bool TryGetServerPrototypes(NetUserId userId, [NotNullWhen(true)] out List<string>? prototypes)
    {
        prototypes = null;
        return false;
    }

    public bool TryGetServerOocColor(NetUserId userId, [NotNullWhen(true)] out Color? color)
    {
        color = null;
        if (!SponsorRoleBenefits.TryGetHighestRole(role => _roles.HasRole(userId, role), out var highest) ||
            !Color.TryFromHex(SponsorRoleBenefits.All[highest].OocColor, out var parsed))
        {
            return false;
        }

        color = parsed;
        return true;
    }

    public int GetServerExtraCharSlots(NetUserId userId)
    {
        return 0;
    }

    public bool HaveServerPriorityJoin(NetUserId userId)
    {
        foreach (var role in PriorityJoinRoles)
        {
            if (_roles.HasRole(userId, role))
                return true;
        }

        return false;
    }

    public Task PreloadServer(NetUserId userId)
    {
        return _roles.Preload(userId);
    }
}
