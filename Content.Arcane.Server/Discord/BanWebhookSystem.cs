using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Content.Arcane.Common.CCVar;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.Discord;
using Content.Server.GameTicking;
using Content.Shared.Database;
using Robust.Server;
using Robust.Server.Player;
using Robust.Shared.Configuration;

namespace Content.Arcane.Server.Discord;

/// <summary>
/// Posts every server and role ban to the Discord webhook in discord.ban_webhook.
/// </summary>
public sealed partial class BanWebhookSystem : EntitySystem
{
    private static readonly Dictionary<NoteSeverity, int> SeverityColors = new()
    {
        { NoteSeverity.High, 12198144 },
        { NoteSeverity.Medium, 12207872 },
        { NoteSeverity.Minor, 14072832 },
        { NoteSeverity.None, 42198 },
    };

    [Dependency] private IBanManager _bans = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private DiscordWebhook _discord = default!;
    [Dependency] private IPlayerManager _players = default!;
    [Dependency] private IBaseServer _server = default!;
    [Dependency] private ServerGameTicker _ticker = default!;

    public override void Initialize()
    {
        base.Initialize();

        if (_bans is BanManager manager)
            manager.OnBanCreated += OnBanCreated;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_bans is BanManager manager)
            manager.OnBanCreated -= OnBanCreated;
    }

    private void OnBanCreated(BanDef ban)
    {
        var url = _cfg.GetCVar(ArcaneCVars.DiscordBanWebhook);
        if (string.IsNullOrEmpty(url))
            return;

        var empty = Loc.GetString("ban-webhook-empty");
        var player = ban.UserIds.Length == 0
            ? empty
            : string.Join(", ", ban.UserIds.Select(id => _players.TryGetPlayerData(id, out var data) ? data.UserName : id.ToString()));
        var admin = ban.BanningAdmin is { } adminId && _players.TryGetPlayerData(adminId, out var adminData)
            ? adminData.UserName
            : empty;
        var expiration = ban.ExpirationTime is { } expires
            ? $"<t:{expires.ToUnixTimeSeconds()}:f>"
            : Loc.GetString("ban-webhook-empty-expiration");
        var roles = ban.Roles is { } roleDefs
            ? string.Concat(roleDefs.Select(role => $"- {role.RoleId}\n"))
            : null;

        var payload = new WebhookPayload
        {
            Username = Loc.GetString("server-ban-webhook-name"),
            Embeds = new List<WebhookEmbed>
            {
                new()
                {
                    Title = Loc.GetString(roles != null ? "role-ban-webhook-title" : "server-ban-webhook-title"),
                    Color = SeverityColors.GetValueOrDefault(ban.Severity, 42198),
                    Description = Loc.GetString(roles != null ? "role-ban-webhook-description" : "server-ban-webhook-description",
                        ("player", player),
                        ("admin", admin),
                        ("expiration", expiration),
                        ("severity", ban.Severity),
                        ("reason", ban.Reason),
                        ("roles", roles ?? string.Empty)),
                    Footer = new WebhookEmbedFooter
                    {
                        Text = Loc.GetString("ban-webhook-footer", ("serverName", _server.ServerName), ("roundId", _ticker.RoundId)),
                    },
                },
            },
        };

        _ = Send(payload, url);
    }

    private async Task Send(WebhookPayload payload, string url)
    {
        try
        {
            if (await _discord.GetWebhook(url) is not { } identifier)
                return;

            Log.Debug(JsonSerializer.Serialize(payload));
            await _discord.CreateMessage(identifier.ToIdentifier(), payload);
        }
        catch (Exception e)
        {
            Log.Error($"Error while sending ban webhook to Discord: {e}");
        }
    }
}
