using Content.Shared.Input;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Client.NamePeek;

public sealed partial class NamePeekSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IUserInterfaceManager _userInterface = default!;
    [Dependency] private IResourceCache _resources = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private NamePeekOverlay _overlay = default!;

    public bool Visible { get; private set; }

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;

        _overlay = new NamePeekOverlay(this, EntityManager, _playerManager, _userInterface, _resources, _prototypes);
        _overlayManager.AddOverlay(_overlay);

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.NamePeek, new PointerInputCmdHandler(OnNamePeek, ignoreUp: false, outsidePrediction: true))
            .Register<NamePeekSystem>();
    }

    public override void Shutdown()
    {
        Visible = false;
        _overlayManager.RemoveOverlay(_overlay);
        _overlay.Dispose();
        CommandBinds.Unregister<NamePeekSystem>();
        base.Shutdown();
    }

    private bool OnNamePeek(in PointerInputCmdHandler.PointerInputCmdArgs args)
    {
        Visible = args.State == BoundKeyState.Down && _playerManager.LocalEntity != null;
        return false;
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        Visible = false;
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        Visible = false;
    }
}
