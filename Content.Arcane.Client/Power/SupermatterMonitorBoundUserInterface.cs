using Content.Arcane.Shared.Power;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Arcane.Client.Power;

[UsedImplicitly]
public sealed class SupermatterMonitorBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private SupermatterMonitorWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<SupermatterMonitorWindow>();
        _window.FocusSelected += OnFocusSelected;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is SupermatterMonitorState monitor)
            _window?.UpdateReadings(monitor);
    }

    private void OnFocusSelected(NetEntity entity) => SendMessage(new SupermatterMonitorSelectMessage(entity));

    protected override void Dispose(bool disposing)
    {
        if (disposing && _window != null)
        {
            _window.FocusSelected -= OnFocusSelected;
            _window.Dispose();
            _window = null;
        }
        base.Dispose(disposing);
    }
}
