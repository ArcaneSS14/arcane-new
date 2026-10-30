using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Examine;
using Content.Shared.Research.Components;
using Content.Shared.Research.Prototypes;

namespace Content.Server.Research.Systems;

/// <summary>
/// Arcane additions: spoken console unlock messages, numbered server names and server examine.
/// </summary>
public sealed partial class ResearchSystem
{
    [Dependency] private ChatSystem _chat = default!;

    private void InitializeArcane()
    {
        SubscribeLocalEvent<ResearchServerComponent, ExaminedEvent>(OnServerExamined);
    }

    private void SayTechnologyUnlocked(EntityUid console, TechnologyPrototype technology, string cost, string approver)
    {
        var message = Loc.GetString("research-console-unlock-technology-ic",
            ("technology", Loc.GetString(technology.Name)),
            ("amount", cost),
            ("approver", approver));

        _chat.TrySendInGameICMessage(console, message, InGameICChatType.Speak, false);
    }
}
