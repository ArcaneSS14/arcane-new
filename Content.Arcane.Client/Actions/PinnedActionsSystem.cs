using Content.Arcane.Shared.Actions;
using Robust.Shared.Player;
using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Client.Actions;

/// <summary>
///     Client side mirror of the action prototypes the local player pinned. The server owns the set, this only
///     keeps it around while the player changes bodies and asks for it again after reconnecting.
/// </summary>
public sealed partial class PinnedActionsSystem : EntitySystem
{
    private readonly Dictionary<EntProtoId, int> _pinned = new();

    /// <summary>
    ///     Raised when the pinned actions changed and the action bar has to be rebuilt.
    /// </summary>
    public event Action? PinsChanged;

    public bool IsPinned(EntProtoId prototype)
    {
        return _pinned.ContainsKey(prototype);
    }

    public IEnumerable<EntProtoId> GetPinned()
    {
        return _pinned.Keys.ToArray();
    }

    public IEnumerable<KeyValuePair<EntProtoId, int>> GetPositions() => _pinned.ToArray();

    public int? GetPosition(EntProtoId prototype) => _pinned.TryGetValue(prototype, out var position) ? position : null;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<PinnedActionsStateMessage>(OnState);
        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
    }

    /// <summary>
    ///     Pins or unpins an action prototype, the pin is kept across bodies and reconnects.
    /// </summary>
    public void SetPinned(EntProtoId prototype, bool pinned, int? position = null, bool notify = true)
    {
        if (pinned)
        {
            var slot = position ?? GetPosition(prototype) ?? PinnedActionPositions.FirstFree(_pinned);
            if (GetPosition(prototype) == slot || !PinnedActionPositions.Place(_pinned, prototype, slot))
                return;
        }
        else if (!_pinned.Remove(prototype))
        {
            return;
        }

        RaiseNetworkEvent(new SetPinnedActionMessage(prototype, pinned, GetPosition(prototype) ?? -1));
        if (notify)
            PinsChanged?.Invoke();
    }

    private void OnState(PinnedActionsStateMessage args)
    {
        if (_pinned.Count == args.Pinned.Count && _pinned.All(pair => args.Pinned.TryGetValue(pair.Key, out var slot) && slot == pair.Value))
            return;

        _pinned.Clear();
        foreach (var (prototype, slot) in args.Pinned)
            _pinned.Add(prototype, slot);

        PinsChanged?.Invoke();
    }

    /// <summary>
    ///     A client has no pins of its own, it asks the server for them whenever it starts playing a body. This
    ///     covers joining, changing bodies and reconnecting, and it resyncs a client that connected elsewhere.
    /// </summary>
    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        RaiseNetworkEvent(new RequestPinnedActionsMessage());
    }
}
