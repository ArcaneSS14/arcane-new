using Content.Arcane.Shared.Bitrunning;
using JetBrains.Annotations;

namespace Content.Arcane.Client.Bitrunning.UI.Disk;

[UsedImplicitly]
public sealed class BitrunningDiskBUI(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private BitrunningDiskWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<BitrunningDiskWindow>();
        _window.OnSelected += option => SendPredictedMessage(new BitrunningDiskSelectOptionMessage(option));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not BitrunningDiskBoundUiState cast || _window == null)
            return;

        _window.SetState(cast.Options, cast.SelectedOption);
    }
}
