// SPDX-License-Identifier: AGPL-3.0-or-later

// Arcane-Start
using Content.Shared.Chat;
using Content.Shared.Cuffs.Components;
using Content.Shared.Popups;

namespace Content.Server.Chat.Systems;

/// <summary>
/// Arcane - no radio while in critical condition or cuffed (Orion #339).
/// </summary>
public sealed partial class ChatSystem
{
    [Dependency] private SharedPopupSystem _arcanePopup = default!;

    /// <summary>
    /// Whether the source can talk on the radio. If not, the message is turned into a plain whisper.
    /// </summary>
    private bool TryUseRadio(EntityUid source, string radioMessage, ref string message, ref InGameICChatType type)
    {
        if (CanUseRadio(source))
            return true;

        message = radioMessage;
        type = InGameICChatType.Whisper;
        return false;
    }

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
// Arcane-End
