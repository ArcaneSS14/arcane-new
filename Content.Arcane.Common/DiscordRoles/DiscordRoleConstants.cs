namespace Content.Arcane.Common.DiscordRoles;

public static class DiscordRoleConstants
{
    /// <summary>
    /// Postgres notification channel the Discord integration notifies with a player id after replacing that
    /// player's role rows.
    /// </summary>
    public const string UpdatedNotificationChannel = "arcane_discord_roles_updated";
}
