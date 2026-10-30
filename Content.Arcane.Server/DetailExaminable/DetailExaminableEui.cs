using Content.Arcane.Shared.DetailExaminable;
using Content.Server.EUI;
using Content.Shared.Eui;

namespace Content.Arcane.Server.DetailExaminable;

public sealed class DetailExaminableEui(DetailExaminableEuiState state) : BaseEui
{
    public override EuiStateBase GetNewState()
    {
        return state;
    }
}
