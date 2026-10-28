// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Arcane.Shared.DetailExaminable;
using Content.Client.Eui;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Arcane.Client.DetailExaminable;

[UsedImplicitly]
public sealed partial class DetailExaminableEui : BaseEui
{
    [Dependency] private IEntityManager _entMan = default!;

    private readonly DetailExaminableWindow _window = new();

    public DetailExaminableEui()
    {
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        base.HandleState(state);

        if (state is DetailExaminableEuiState detail)
            _window.UpdateState(detail, _entMan);
    }
}
