using System.Linq;
using Content.Arcane.Shared.Polls;
using Content.Goobstation.Server.Polls;
using Content.Goobstation.Shared.Polls;
using Content.Server.Database;
using Robust.Shared.Network;

namespace Content.Arcane.Server.Polls;

/// <summary>
/// Records views using the authenticated sender and polls actually offered to that connection.
/// Voting and poll administration remain owned by the existing poll manager.
/// </summary>
public sealed partial class PollSeenSystem : EntitySystem
{
    [Dependency] private PollManager _polls = default!;
    [Dependency] private IServerDbManager _db = default!;
    [Dependency] private INetManager _net = default!;

    private readonly Dictionary<INetChannel, HashSet<int>> _offered = new();
    private readonly Dictionary<INetChannel, HashSet<int>> _confirmed = new();
    private readonly HashSet<(INetChannel Channel, int Poll)> _pending = new();

    public override void Initialize()
    {
        base.Initialize();
        _polls.PollsSent += OnPollsSent;
        _net.Disconnect += OnDisconnect;
        SubscribeNetworkEvent<MarkPollSeenEvent>(OnSeen);
    }

    public override void Shutdown()
    {
        _polls.PollsSent -= OnPollsSent;
        _net.Disconnect -= OnDisconnect;
        _offered.Clear();
        _confirmed.Clear();
        _pending.Clear();
        base.Shutdown();
    }

    private void OnDisconnect(object? sender, NetDisconnectedArgs args)
    {
        _offered.Remove(args.Channel);
        _confirmed.Remove(args.Channel);
    }

    private void OnPollsSent(INetChannel channel, IReadOnlyList<PollData> polls)
    {
        if (!channel.IsConnected)
            return;
        var now = DateTime.UtcNow;
        _offered[channel] = polls.Where(p => p.Active
            && (p.EndTime == null || p.EndTime > now)).Select(p => p.PollId).ToHashSet();
        if (_confirmed.TryGetValue(channel, out var confirmed))
            confirmed.IntersectWith(_offered[channel]);
    }

    private async void OnSeen(MarkPollSeenEvent message, EntitySessionEventArgs args)
    {
        var channel = args.SenderSession.Channel;
        var key = (channel, message.PollId);
        if (!_offered.TryGetValue(channel, out var offered) || !offered.Contains(message.PollId))
        {
            RaiseNetworkEvent(new PollSeenAcknowledgedEvent(message.PollId, false), channel);
            return;
        }
        if (_confirmed.TryGetValue(channel, out var confirmed) && confirmed.Contains(message.PollId))
        {
            RaiseNetworkEvent(new PollSeenAcknowledgedEvent(message.PollId, true), channel);
            return;
        }
        if (!_pending.Add(key))
            return;

        var acknowledged = false;
        try
        {
            var user = args.SenderSession.UserId;
            var added = await _db.MarkPollSeenAsync(message.PollId, user);
            var seen = added || (await _db.GetSeenPollIdsAsync(user)).Contains(message.PollId);
            if (channel.IsConnected)
            {
                // Only remember a successful database result for a still-tracked connection.
                if (seen && _offered.ContainsKey(channel))
                {
                    if (!_confirmed.TryGetValue(channel, out var confirmedViews))
                    {
                        confirmedViews = new HashSet<int>();
                        _confirmed.Add(channel, confirmedViews);
                    }
                    confirmedViews.Add(message.PollId);
                }
                RaiseNetworkEvent(new PollSeenAcknowledgedEvent(message.PollId, seen), channel);
                acknowledged = true;
            }
            if (added)
            {
                var count = await _db.GetPollSeenCountAsync(message.PollId);
                RaiseNetworkEvent(new PollSeenCountChangedEvent(message.PollId, count));
            }
        }
        catch (Exception exception)
        {
            Log.Error($"Failed to record view of poll {message.PollId}: {exception}");
            if (!acknowledged && channel.IsConnected)
                RaiseNetworkEvent(new PollSeenAcknowledgedEvent(message.PollId, false), channel);
        }
        finally
        {
            _pending.Remove(key);
        }
    }
}
