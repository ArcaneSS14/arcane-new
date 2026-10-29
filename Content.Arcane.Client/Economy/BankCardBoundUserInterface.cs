using Content.Arcane.Shared.Economy;
using Robust.Client.UserInterface;

namespace Content.Arcane.Client.Economy;

public sealed partial class BankCardBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private BankCardWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BankCardWindow>();
        _window.OnWithdraw += amount => SendMessage(new BankCardWithdrawMessage(amount));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is BankCardBoundUiState cast)
            _window?.UpdateState(cast);
    }
}
