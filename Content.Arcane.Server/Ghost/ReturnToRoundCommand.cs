// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Arcane.Server.Ghost;

[AnyCommand]
public sealed partial class ReturnToRoundCommand : LocalizedEntityCommands
{
    [Dependency] private GhostReturnToRoundSystem _returnToRound = default!;

    public override string Command => "returntoround";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { AttachedEntity: { } ghost } player)
        {
            shell.WriteError(Loc.GetString("ghost-respawn-command-no-entity"));
            return;
        }

        _returnToRound.TryReturnToRound(ghost, player);
    }
}
