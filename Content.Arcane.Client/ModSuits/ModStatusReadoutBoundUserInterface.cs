using Content.Arcane.Shared.ModSuits;
using JetBrains.Annotations;

namespace Content.Arcane.Client.ModSuits;

[UsedImplicitly]
public sealed class ModStatusReadoutBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ModStatusReadoutWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<ModStatusReadoutWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is ModStatusReadoutState readout)
            _window?.UpdateReadout(readout);
    }
}
