// SPDX-License-Identifier: AGPL-3.0-or-later

// Arcane-Start
using Content.Goobstation.Shared.Clothing.Components;
using Content.Shared.Whitelist;

namespace Content.Goobstation.Shared.Clothing.Systems;

/// <summary>
/// Arcane - coating blacklist (Orion #437).
/// </summary>
public sealed partial class ClothingCoatingSystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    private bool IsCoatingBlocked(Entity<ClothingCoatingComponent> ent, EntityUid target, EntityUid user)
    {
        if (!_whitelist.IsWhitelistPass(ent.Comp.Blacklist, target))
            return false;

        _popup.PopupClient(Loc.GetString("clothing-coating-blocked", ("target", target), ("source", ent.Owner)), target, user);
        return true;
    }
}
// Arcane-End
