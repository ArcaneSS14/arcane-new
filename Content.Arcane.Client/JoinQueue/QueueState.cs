using Content.Arcane.Shared.JoinQueue;
using Robust.Client.Console;
using Robust.Client.State;
using Robust.Client.UserInterface;

namespace Content.Arcane.Client.JoinQueue;

public sealed partial class QueueState : State
{
    [Dependency] private IUserInterfaceManager _userInterface = default!;
    [Dependency] private IClientConsoleHost _console = default!;



    private QueueGui? _gui;


    protected override void Startup()
    {
        _gui = new QueueGui();
        _userInterface.StateRoot.AddChild(_gui);

        _gui.QuitPressed += OnQuitPressed;
    }

    protected override void Shutdown()
    {
        _gui!.QuitPressed -= OnQuitPressed;
        _gui.Dispose();

    }


    public void OnQueueUpdate(QueueUpdateMessage msg)
    {
        _gui?.UpdateInfo(msg);
    }

    private void OnQuitPressed()
    {
        _console.ExecuteCommand("disconnect");
    }


}
