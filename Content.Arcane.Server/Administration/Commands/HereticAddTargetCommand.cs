using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Trauma.Shared.Heretic.Components;
using Robust.Shared.Console;

namespace Content.Arcane.Server.Administration.Commands;

/// <summary>
/// Adds a humanoid to a heretic's sacrifice targets.
/// </summary>
[AdminCommand(AdminFlags.Debug)]
public sealed partial class HereticAddTargetCommand : LocalizedEntityCommands
{
    [Dependency] private SharedMindSystem _mind = default!;

    public override string Command => "hereticaddtarget";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netHeretic)
            || !EntityManager.TryGetEntity(netHeretic, out var heretic)
            || !_mind.TryGetMind(heretic.Value, out var mindId, out _)
            || !EntityManager.TryGetComponent<HereticComponent>(mindId, out var hereticComp))
        {
            shell.WriteError(Loc.GetString("cmd-hereticaddtarget-invalid-heretic", ("entity", args[0])));
            return;
        }

        if (!NetEntity.TryParse(args[1], out var netTarget)
            || !EntityManager.TryGetEntity(netTarget, out var target)
            || !EntityManager.HasComponent<HumanoidProfileComponent>(target))
        {
            shell.WriteError(Loc.GetString("cmd-hereticaddtarget-invalid-target", ("entity", args[1])));
            return;
        }

        if (hereticComp.SacrificeTargets.Exists(x => x.Entity == netTarget))
            return;

        hereticComp.SacrificeTargets.Add(new SacrificeTargetData
        {
            Entity = netTarget,
            Name = EntityManager.GetComponent<MetaDataComponent>(target.Value).EntityName,
        });
        EntityManager.Dirty(mindId, hereticComp);

        shell.WriteLine(Loc.GetString("cmd-hereticaddtarget-success", ("entity", args[1])));
    }
}
