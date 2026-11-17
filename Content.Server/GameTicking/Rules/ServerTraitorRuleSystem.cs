using Content.Server.Objectives;
using Content.Server.Traitor.Uplink;
using Content.Shared.FixedPoint;
using Content.Shared.PDA;
using Content.Shared.GameTicking.Rules;
using Content.Shared.GameTicking.Rules.Components;

namespace Content.Server.GameTicking.Rules;

public sealed partial class ServerTraitorRuleSystem : TraitorRuleSystem
{
    [Dependency] private UplinkSystem _uplink = default!;

    public override void Initialize()
    {
        base.Initialize();

        Log.Level = LogLevel.Debug;
    }

    protected override (Note[]? Code, bool ImplantIssued) RequestUplink(EntityUid traitor, FixedPoint2 startingBalance) // Arcane-Edit: return uplink data for the shared briefing
    {
        var pda = _uplink.FindUplinkTarget(traitor);

        Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Uplink add");
        var uplinked = _uplink.AddUplink(traitor, startingBalance, out var code, pda, giveDiscounts: true, bindToPda: false);

        if (code != null && uplinked == AddUplinkResult.Pda)
        {
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Uplink is PDA");

            return (code, false); // Arcane-Edit: return the PDA code
        }

        if (uplinked == AddUplinkResult.Implant)
        {
            Log.Debug($"MakeTraitor {ToPrettyString(traitor)} - Uplink is implant");
            return (null, true); // Arcane-Edit: report fallback implant delivery
        }


        // Arcane-Edit-Start
        Log.Error($"MakeTraitor failed on {ToPrettyString(traitor)} - No uplink could be added");
        return (null, false);
        // Arcane-Edit-End
    }

    // TODO: AntagCodewordsComponent
    [SubscribeLocalEvent]
    private void OnObjectivesTextPrepend(EntityUid uid, TraitorRuleComponent comp, ref ObjectivesTextPrependEvent args)
    {
        if(comp.GiveCodewords)
            args.Text += "\n" + Loc.GetString("traitor-round-end-codewords", ("codewords", string.Join(", ", Codeword.GetCodewords(comp.CodewordFactionPrototypeId))));
    }
}
