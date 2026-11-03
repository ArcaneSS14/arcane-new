using Content.Arcane.Server.ResponseForce;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Arcane.Shared.ResponseForce;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Server.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class CallResponseForceCommand : IConsoleCommand
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private EntityManager _entManager = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    public string Command => "callresponseforce";
    public string Description => "Вызов команды спецсил";
    public string Help => "Использование: callresponseforce <ResponseForceTeamPrototypeId>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number"));
            shell.WriteLine(Help);
            return;
        }

        if (!_prototypes.HasIndex<ResponseForceTeamPrototype>(args[0]))
        {
            shell.WriteError(Loc.GetString("response-force-command-unknown-team", ("team", args[0])));
            return;
        }

        var responseForceSystem = _entManager.System<ResponseForceSystem>();
        var caller = shell.Player != null ? shell.Player.Name : "An administrator";
        var result = responseForceSystem.CallResponseForce(args[0], caller);
        if (result != ResponseForceCallResult.Success)
        {
            shell.WriteError(result switch
            {
                ResponseForceCallResult.OnCooldown => Loc.GetString("response-force-command-cooldown", ("time", responseForceSystem.DelayTime)),
                ResponseForceCallResult.NotInRound => Loc.GetString("response-force-command-not-in-round"),
                ResponseForceCallResult.ShuttleLoadFailed => Loc.GetString("response-force-command-shuttle-failed"),
                _ => Loc.GetString("response-force-command-invalid-team", ("team", args[0])),
            });
            return;
        }

        _adminLogger.Add(
            LogType.AdminMessage,
            LogImpact.Extreme,
            $"Admin {caller} called SpecForceTeam {args[0]}.");
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        // Index all Response Force Teams prototypes and write them down in completion result.
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<ResponseForceTeamPrototype>(true, _prototypes),
                "Тип вызова"),
            _ => CompletionResult.Empty,
        };
    }
}
