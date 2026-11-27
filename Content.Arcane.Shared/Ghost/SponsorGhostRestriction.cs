using System.Diagnostics.CodeAnalysis;
using Content.Arcane.Shared.DiscordRoles;
using Content.Arcane.Shared.CustomGhost;
using Robust.Shared.Player;

namespace Content.Arcane.Shared.Ghost;

[DataDefinition]
public sealed partial class SponsorGhostRestriction : CustomGhostRestriction
{
    [DataField]
    public DiscordRole Role = DiscordRole.SponsorTier2;

    public override bool CanUse(ICommonSession player, [NotNullWhen(false)] out string? failReason)
    {
        var roles = IoCManager.Resolve<ISharedDiscordRoleManager>();
        if (roles.HasRole(player, Role) || roles.HasRole(player, DiscordRole.AdminBenefit))
        {
            failReason = null;
            return true;
        }

        failReason = Loc.GetString("custom-ghost-fail-sponsor-tier-two");
        return false;
    }
}
