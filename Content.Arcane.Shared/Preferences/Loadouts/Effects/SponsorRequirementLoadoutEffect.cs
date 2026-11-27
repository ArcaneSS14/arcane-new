using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Arcane.Shared.DiscordRoles;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Arcane.Shared.Preferences.Loadouts.Effects;

/// <summary>
/// Only players with one of the given Discord roles can pick this sponsor loadout.
/// </summary>
public sealed partial class SponsorRequirementLoadoutEffect : LoadoutEffect
{
    [DataField(required: true)]
    public HashSet<DiscordRole> Roles;

    public override bool Validate(
        HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        LoadoutPrototype proto,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        if (session == null)
        {
            reason = FormattedMessage.Empty;
            return false;
        }

        var discordRoles = collection.Resolve<ISharedDiscordRoleManager>();
        var hasRole = Roles.Any(role => discordRoles.HasRole(session, role));

        reason = FormattedMessage.FromUnformatted(Loc.GetString("loadout-sponsor-requirement"));
        return hasRole;
    }
}
