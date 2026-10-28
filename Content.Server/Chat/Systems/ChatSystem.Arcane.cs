// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Cuffs.Components;
using Content.Shared.Popups;

namespace Content.Server.Chat.Systems;

/// <summary>
/// Arcane - no radio while in critical condition or cuffed (Orion #339).
/// </summary>
public sealed partial class ChatSystem
{
    [Dependency] private SharedPopupSystem _arcanePopup = default!;

    private bool CanUseRadio(EntityUid source)
    {
        if (_mobStateSystem.IsCritical(source))
        {
            _arcanePopup.PopupEntity(Loc.GetString("chat-manager-cannot-radio-while-critical"), source, source);
            return false;
        }

        if (TryComp<CuffableComponent>(source, out var cuffable) && cuffable.CuffedHandCount > 0)
        {
            _arcanePopup.PopupEntity(Loc.GetString("chat-manager-cannot-radio-with-bound-hands"), source, source);
            return false;
        }

        return true;
    }
}
