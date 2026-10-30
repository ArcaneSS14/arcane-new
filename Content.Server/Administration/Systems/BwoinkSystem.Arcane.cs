using System.Linq;
using Content.Server.Administration.Managers;
using Content.Shared.Database;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server.Administration.Systems;

/// <summary>
/// AHelp anti-spam: a short per-sender cooldown and a temporary ban for repeating the same message.
/// </summary>
public sealed partial class BwoinkSystem
{
    [Dependency] private IBanManager _banManager = default!;

    private static readonly TimeSpan AntiSpamCooldown = TimeSpan.FromSeconds(1);
    private const int AntiSpamMaxRecentMessages = 10;
    private const int AntiSpamRepeatCount = 5;
    private const int AntiSpamBanMinutes = 10;

    private readonly Dictionary<NetUserId, Queue<(string Text, TimeSpan Timestamp)>> _recentAHelpMessages = new();

    /// <summary>
    /// Returns true if the message must be dropped. Bans the sender if they keep repeating the same text.
    /// </summary>
    private bool CheckAHelpSpam(ICommonSession sender, string text)
    {
        var now = _timing.RealTime;
        if (!_recentAHelpMessages.TryGetValue(sender.UserId, out var queue))
        {
            queue = new Queue<(string, TimeSpan)>();
            _recentAHelpMessages[sender.UserId] = queue;
        }

        if (queue.Count > 0 && now - queue.Last().Timestamp < AntiSpamCooldown)
            return true;

        var isSpam = queue.Count >= AntiSpamRepeatCount && queue.All(msg => msg.Text == text);

        queue.Enqueue((text, now));
        if (queue.Count > AntiSpamMaxRecentMessages)
            queue.Dequeue();

        if (!isSpam)
            return false;

        queue.Clear();

        var ban = new CreateServerBanInfo(Loc.GetString("bwoink-anti-spam-ban-reason"));
        ban.AddUser(sender.UserId, sender.Name);
        ban.WithMinutes(AntiSpamBanMinutes);
        ban.WithSeverity(NoteSeverity.High);
        _banManager.CreateServerBan(ban);
        return true;
    }
}
