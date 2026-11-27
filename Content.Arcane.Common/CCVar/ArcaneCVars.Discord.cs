using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCVars
{
    /// <summary>
    /// Discord webhook that gets a message for every server and role ban. Empty disables it.
    /// </summary>
    public static readonly CVarDef<string> DiscordBanWebhook =
        CVarDef.Create("discord.ban_webhook", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);
}
