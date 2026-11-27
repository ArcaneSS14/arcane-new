using Robust.Shared.Player;

namespace Content.Arcane.Shared.DiscordRoles;

public interface ISharedDiscordRoleManager
{
    event Action? RolesUpdated;

    bool HasRole(ICommonSession session, DiscordRole role);

    bool HasAnyRole(ICommonSession session, HashSet<DiscordRole> roles);
}
