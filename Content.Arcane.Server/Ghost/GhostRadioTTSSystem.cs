using Content.Shared.Actions;
using Content.Shared.Ghost;
using Content.Shared.Ghost.Components;
using Robust.Shared.GameObjects;

namespace Content.Arcane.Server.Ghost;

public sealed partial class GhostRadioTTSSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    private const string ToggleGhostRadioTTSAction = "ActionToggleGhostRadioTTS";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostComponent, ComponentInit>(OnGhostComponentInit);
    }

    private void OnGhostComponentInit(EntityUid uid, GhostComponent component, ComponentInit args)
    {
        EntityUid? actionId = null;
        _actions.AddAction(uid, ref actionId, ToggleGhostRadioTTSAction);
    }
}
