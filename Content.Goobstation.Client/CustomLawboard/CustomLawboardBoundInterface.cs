// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.CustomLawboard;
using Content.Shared.Silicons.Laws;
using JetBrains.Annotations;

namespace Content.Goobstation.Client.CustomLawboard;

/// <summary>
/// Initializes a <see cref="LawboardSiliconLawUi"/> and updates it when new server messages are received.
/// </summary>
[UsedImplicitly]
public sealed class CustomLawboardBoundInterface : BoundUserInterface
{
    [ViewVariables]
    private LawboardSiliconLawUi? _window;

    public CustomLawboardBoundInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<LawboardSiliconLawUi>();

        _window.LawsChangedEvent += (laws, popup) => OnLawsChanged(laws, popup);

        // Arcane: opening the editor must never send an empty or stale replacement to the server.
        if (EntMan.TryGetComponent<CustomLawboardComponent>(Owner, out var board))
            _window.SetLaws(SharedCustomLawboardSystem.SanitizeLaws(board.Laws), notify: false);
        Update();
    }

    private void OnLawsChanged(List<SiliconLaw> value, bool popup)
    {
        SendPredictedMessage(new CustomLawboardChangeLawsMessage(value, popup));
    }
}
