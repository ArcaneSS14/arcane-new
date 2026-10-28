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

    private void SetArcaneServerName(Entity<ResearchServerComponent> ent)
    {
        ent.Comp.ServerName = Loc.GetString("research-server-name", ("id", ent.Comp.Id.ToString("D4")));
    }

    private void SayTechnologyUnlocked(EntityUid console, TechnologyPrototype technology, string approver)
    {
        var message = Loc.GetString("research-console-unlock-technology-ic",
            ("technology", Loc.GetString(technology.Name)),
            ("amount", technology.Cost),
            ("approver", approver));

        _chat.TrySendInGameICMessage(console, message, InGameICChatType.Speak, false);
    }

    private void OnServerExamined(Entity<ResearchServerComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("research-server-examine",
            ("name", ent.Comp.ServerName),
            ("points", GetPointsPerSecond(ent, ent.Comp))));
    }
}
