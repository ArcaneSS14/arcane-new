// Arcane-Start
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed partial class ChatUIController
{
    private bool TryGetSavedRoleHighlights(ProtoId<JobPrototype> job, out string? highlights)
    {
        var ev = new GetRoleHighlightsEvent(job);
        _ent.EventBus.RaiseEvent(EventSource.Local, ref ev);
        highlights = ev.Highlights;
        return !string.IsNullOrWhiteSpace(highlights);
    }
}
// Arcane-End
