using System.Linq;
using System.Text;
using Content.Shared.Antag;
using Content.Shared.Codewords;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Mind;
using Content.Shared.NPC.Systems;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Roles.Jobs;
using Content.Shared.Roles.RoleCodeword;
using Robust.Shared.Random;

namespace Content.Shared.GameTicking.Rules;

public abstract partial class TraitorRuleSystem : GameRuleSystem<TraitorRuleComponent>
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] protected CodewordSystem Codeword = default!;
    [Dependency] private NpcFactionSystem _npcFaction = default!;
    [Dependency] private SharedJobSystem _jobs = default!;
    [Dependency] private SharedMindSystem _mindSystem = default!;
    [Dependency] private SharedRoleCodewordSystem _roleCodewordSystem = default!;
    [Dependency] private SharedRoleSystem _roleSystem = default!;

    private static readonly Color TraitorCodewordColor = Color.FromHex("#cc3b3b");

    [SubscribeLocalEvent]
    private void AfterEntitySelected(Entity<TraitorRuleComponent> rule, ref AfterAntagEntitySelectedEvent args)
    {
        Log.Debug($"AfterAntagEntitySelected {ToPrettyString(rule)}");
        MakeTraitor(args.EntityUid, rule);
    }

    public bool MakeTraitor(EntityUid traitor, Entity<TraitorRuleComponent> rule)
    {
        Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - start");
        var factionCodewords = rule.Comp.GiveCodewords ? Codeword.GetCodewords(rule.Comp.CodewordFactionPrototypeId) : []; // Arcane-Edit: always > only when enabled

        //Grab the mind if it wasn't provided
        if (!_mindSystem.TryGetMind(traitor, out var mindId, out var mind))
        {
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)}  - failed, no Mind found");
            return false;
        }

        // Arcane: keep the localization key stable across server languages.
        var issuers = ProtoMan.Index(rule.Comp.ObjectiveIssuers).Values;
        var issuer = issuers.Count > 0 ? _random.Pick(issuers) : "objective-issuer-unknown";

        // Uplink code will go here if applicable, but we still need the variable if there aren't any
        Note[]? code = null;
        var implantIssued = false; // Arcane

        if (rule.Comp.GiveUplink)
        {
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Uplink start");
            // Calculate the amount of currency on the uplink.
            var startingBalance = rule.Comp.StartingBalance;
            if (_jobs.MindTryGetJob(mindId, out var prototype))
            {
                if (startingBalance < prototype.AntagAdvantage) // Can't use Math functions on FixedPoint2
                    startingBalance = 0;
                else
                    startingBalance = startingBalance - prototype.AntagAdvantage;
            }

            // Choose and generate an Uplink, and return the uplink code if applicable
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Uplink request start");
            (code, implantIssued) = RequestUplink(traitor, startingBalance); // Arcane
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Uplink request completed");
        }

        if (rule.Comp.GiveBriefing)
        {
            _antag.SendBriefing(traitor, GenerateBriefing(factionCodewords, code, issuer, implantIssued, characterPanel: false), Color.Crimson, rule.Comp.GreetSoundNotification); // Arcane
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Sent the Briefing");
        }

        Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Adding TraitorMind");
        rule.Comp.TraitorMinds.Add(mindId);

        // Assign briefing
        //Since this provides neither an antag/job prototype, nor antag status/roletype,
        //and is intrinsically related to the traitor role
        //it does not need to be a separate Mind Role Entity
        _roleSystem.MindHasRole<TraitorRoleComponent>((mindId, mind), out var traitorRole);
        if (traitorRole is not null)
        {
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Add traitor briefing components");
            EnsureComp<RoleBriefingComponent>(traitorRole.Value.Owner, out var briefingComp);
            briefingComp.Briefing = GenerateBriefing(factionCodewords, code, issuer, implantIssued, characterPanel: true); // Arcane
        }
        else
        {
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - did not get traitor briefing");
        }

        var color = TraitorCodewordColor; // Fall back to a dark red Syndicate color if a prototype is not found

        // The mind entity is stored in nullspace with a PVS override for the owner, so only they can see the codewords.
        // Arcane-Start: honor rules with codewords disabled.
        if (rule.Comp.GiveCodewords)
        {
            var codewordComp = EnsureComp<RoleCodewordComponent>(mindId);
            _roleCodewordSystem.SetRoleCodewords((mindId, codewordComp), "traitor", factionCodewords.ToList(), color);
        }
        // Arcane-End

        // Change the faction
        Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Change faction");
        _npcFaction.RemoveFaction(traitor, rule.Comp.NanoTrasenFaction, false);
        _npcFaction.AddFaction(traitor, rule.Comp.SyndicateFaction);

        Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Finished");
        return true;
    }

    protected virtual (Note[]? Code, bool ImplantIssued) RequestUplink(EntityUid traitor, FixedPoint2 startingBalance) => default; // Arcane

    // TODO: figure out how to handle this? add priority to briefing event?
    private string GenerateBriefing(string[] codewords, Note[]? uplinkCode, string objectiveIssuer, bool implantIssued, bool characterPanel)
    {
        // Arcane-Edit-Start: use the assigned corporation and actual uplink result in both views.
        var prefix = $"arcane-traitor-{objectiveIssuer}";
        var sb = new StringBuilder();
        sb.AppendLine(Loc.TryGetString($"{prefix}-intro", out var intro)
            ? intro
            : Loc.GetString("arcane-traitor-generic-intro", ("corporation", Loc.GetString(objectiveIssuer))));
        sb.AppendLine();

        if (!characterPanel && (uplinkCode != null || implantIssued) && Loc.TryGetString($"{prefix}-uplink", out var uplink))
            sb.AppendLine(uplink);

        if (uplinkCode != null)
            sb.AppendLine(Loc.GetString(characterPanel ? "traitor-role-uplink-code-short" : "traitor-role-uplink-code",
                ("code", string.Join("-", uplinkCode).Replace("sharp", "#"))));
        else if (implantIssued)
            sb.AppendLine(Loc.GetString(characterPanel ? "traitor-role-uplink-implant-short" : "traitor-role-uplink-implant"));
        else
            sb.AppendLine(Loc.GetString("traitor-role-no-uplink"));

        if (codewords.Length > 0)
        {
            sb.AppendLine();
            sb.AppendLine(Loc.GetString(characterPanel ? "traitor-role-codewords-short" : "traitor-role-codewords",
                ("codewords", string.Join(", ", codewords))));
        }

        if (characterPanel && Loc.TryGetString($"{prefix}-allies", out var allies) && Loc.TryGetString($"{prefix}-goal", out var goal))
        {
            sb.AppendLine();
            sb.AppendLine(Loc.GetString("arcane-traitor-allegiances"));
            sb.AppendLine(allies);
            sb.AppendLine();
            sb.AppendLine(Loc.GetString("arcane-traitor-notes"));
            sb.AppendLine(goal);
        }
        else if (!characterPanel)
        {
            sb.AppendLine();
            sb.AppendLine(Loc.GetString("arcane-traitor-more-info"));
        }

        return sb.ToString();
        // Arcane-Edit-End
    }

    public List<(EntityUid Id, MindComponent Mind)> GetOtherTraitorMindsAliveAndConnected(MindComponent ourMind)
    {
        List<(EntityUid Id, MindComponent Mind)> allTraitors = new();

        var query = EntityQueryEnumerator<TraitorRuleComponent>();
        while (query.MoveNext(out var uid, out var traitor))
        {
            foreach (var role in GetOtherTraitorMindsAliveAndConnected(ourMind, (uid, traitor)))
            {
                if (!allTraitors.Contains(role))
                    allTraitors.Add(role);
            }
        }

        return allTraitors;
    }

    private List<(EntityUid Id, MindComponent Mind)> GetOtherTraitorMindsAliveAndConnected(MindComponent ourMind, Entity<TraitorRuleComponent> rule)
    {
        var traitors = new List<(EntityUid Id, MindComponent Mind)>();
        foreach (var mind in _antag.GetAntagMinds(rule.Owner))
        {
            if (mind.Comp == ourMind)
                continue;

            traitors.Add((mind, mind));
        }

        return traitors;
    }
}
