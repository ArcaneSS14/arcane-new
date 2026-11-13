using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Arcane.Server.Voodoo;

[AdminCommand(AdminFlags.Fun)]
public sealed partial class VoodooTargetCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entities = default!;

    public string Command => "voodootarget";
    public string Description => Loc.GetString("voodoo-command-description");
    public string Help => Loc.GetString("voodoo-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } actor || args.Length != 2 ||
            !NetEntity.TryParse(args[0], out var dollNet) || !_entities.TryGetEntity(dollNet, out var doll))
        {
            shell.WriteError(Help);
            return;
        }

        EntityUid? target = null;
        if (args[1] != "clear")
        {
            if (!NetEntity.TryParse(args[1], out var targetNet) || !_entities.TryGetEntity(targetNet, out target))
            {
                shell.WriteError(Help);
                return;
            }
        }

        if (!_entities.System<VoodooSystem>().TrySetAdminTarget(actor, doll.Value, target))
            shell.WriteError(Loc.GetString("voodoo-command-invalid"));
        else
            shell.WriteLine(Loc.GetString("voodoo-command-updated"));
    }
}
