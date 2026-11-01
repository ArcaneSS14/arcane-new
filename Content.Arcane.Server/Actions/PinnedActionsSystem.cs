using Content.Arcane.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Players;

namespace Content.Arcane.Server.Actions;

/// <summary>
/// Stores action pins in player data across reconnects, without granting the actions themselves.
/// </summary>
public sealed partial class PinnedActionsSystem : EntitySystem
{
    private const int MaxPinned = 32;

    [Dependency] private IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<SetPinnedActionMessage>(OnSetPinned);
        SubscribeNetworkEvent<RequestPinnedActionsMessage>(OnRequest);
    }

    private void OnSetPinned(SetPinnedActionMessage message, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (session.Data.ContentData() is not { } data)
            return;

        if (!string.IsNullOrWhiteSpace(message.Prototype.Id)
            && _prototypes.TryIndex(message.Prototype, out var prototype)
            && !prototype.Abstract
            && prototype.TryGetComponent<ActionComponent>(out _, EntityManager.ComponentFactory))
        {
            if (!message.Pinned)
                data.PinnedActions.Remove(message.Prototype);
            else if (data.PinnedActions.Count < MaxPinned)
                data.PinnedActions.Add(message.Prototype);
        }

        // Rejections also resynchronize the optimistic client.
        RaiseNetworkEvent(new PinnedActionsStateMessage(new(data.PinnedActions)), session.Channel);
    }

    private void OnRequest(RequestPinnedActionsMessage message, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        var pinned = session.Data.ContentData()?.PinnedActions;
        RaiseNetworkEvent(new PinnedActionsStateMessage(pinned == null ? new() : new(pinned)), session.Channel);
    }
}
