using System.Linq;
using Content.Arcane.Shared.Polls;
using Content.Goobstation.Client.Polls;
using Content.Goobstation.Client.Polls.UI;
using Content.Goobstation.Shared.Polls;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Arcane.Client.Polls;

/// <summary>
/// Tracks poll views separately from votes. Opening a window never marks an old cache as seen.
/// </summary>
public sealed partial class PollSeenSystem : EntitySystem
{
    [Dependency] private PollManager _polls = default!;
    [Dependency] private IClientNetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly HashSet<int> _seen = new();
    private readonly HashSet<int> _shown = new();
    private INetChannel? _requestedChannel;
    private bool _stateReady;
    private bool _lastUnseen;
    private TimeSpan _nextTimeCheck;

    public event Action? OnUnseenChanged;

    public bool HasUnseenPolls => _stateReady && _polls.ActivePolls.Values.Any(p => IsAvailable(p) && !_seen.Contains(p.PollId));

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);
        if (_timing.RealTime < _nextTimeCheck)
            return;
        _nextTimeCheck = _timing.RealTime + TimeSpan.FromSeconds(1);
        RefreshUnseen();
    }

    private void RefreshUnseen()
    {
        var unseen = HasUnseenPolls;
        if (_lastUnseen == unseen)
            return;
        _lastUnseen = unseen;
        OnUnseenChanged?.Invoke();
    }

    public override void Initialize()
    {
        base.Initialize();
        _polls.SeenPollIdsReceived += OnSeenIds;
        _polls.OnActivePollsUpdated += OnPolls;
        _polls.OnPollUpdated += OnPoll;
        _polls.OnPollClosed += OnClosed;
        PollVotingWindow.PollDisplayed += OnDisplayed;
        _net.Disconnect += OnDisconnect;
        SubscribeNetworkEvent<PollSeenAcknowledgedEvent>(OnAcknowledged);
    }

    public override void Shutdown()
    {
        _polls.SeenPollIdsReceived -= OnSeenIds;
        _polls.OnActivePollsUpdated -= OnPolls;
        _polls.OnPollUpdated -= OnPoll;
        _polls.OnPollClosed -= OnClosed;
        PollVotingWindow.PollDisplayed -= OnDisplayed;
        _net.Disconnect -= OnDisconnect;
        base.Shutdown();
    }

    public void RequestActivePolls()
    {
        if (_net.ServerChannel is not { } channel || _requestedChannel == channel)
            return;
        _requestedChannel = channel;
        _polls.RequestActivePolls();
    }

    private static bool IsAvailable(PollData poll)
    {
        var now = DateTime.UtcNow;
        return poll.Active && poll.StartTime <= now && (poll.EndTime == null || poll.EndTime > now);
    }

    private void OnPolls(List<PollData> polls)
    {
        _stateReady = false;
        _shown.Clear();
    }

    private void OnSeenIds(IReadOnlyList<int> ids)
    {
        _seen.Clear();
        _seen.UnionWith(ids);
        _stateReady = true;
        foreach (var id in _shown.ToArray())
            OnDisplayed(id);
        RefreshUnseen();
    }

    private void OnDisplayed(int id)
    {
        if (!_polls.ActivePolls.TryGetValue(id, out var poll) || !IsAvailable(poll))
            return;
        _shown.Add(id);
        if (!_stateReady || !_seen.Add(id))
            return;
        RaiseNetworkEvent(new MarkPollSeenEvent(id));
        RefreshUnseen();
    }

    private void OnAcknowledged(PollSeenAcknowledgedEvent message)
    {
        if (message.Seen)
            _seen.Add(message.PollId);
        else
            _seen.Remove(message.PollId);
        RefreshUnseen();
    }

    private void OnPoll(PollData poll) => RefreshUnseen();
    private void OnClosed(int id) => RefreshUnseen();

    private void OnDisconnect(object? sender, NetDisconnectedArgs args)
    {
        _requestedChannel = null;
        _stateReady = false;
        _seen.Clear();
        _shown.Clear();
        RefreshUnseen();
    }
}
