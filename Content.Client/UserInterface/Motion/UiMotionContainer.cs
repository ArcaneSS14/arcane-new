// Arcane-Start
using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.UserInterface.Motion;

/// <summary>
/// Opt-in host for a single UI subtree. Layout moves the real controls, keeping hit testing aligned.
/// The last rendered image can outlive the subtree without retaining its input or gameplay state.
/// </summary>
public sealed partial class UiMotionContainer : Control
{
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    private static readonly ProtoId<ShaderPrototype> CompositeShader = "UiMotionPremultiplied";

    private readonly UiMotionTransition _transition;
    private readonly Action _capture;
    private IRenderTexture? _texture;
    private IRenderHandle? _renderHandle;
    private Control? _renderChild;
    private UiMotionSnapshot? _exit;
    private float _renderedOpacity;
    private float _renderedOffset;
    private Color _renderedTint = Color.White;
    private ShaderInstance? _shader;

    public UiMotionProfile Profile { get; set; } = UiMotionProfile.Standard;
    public Vector2 VisualOffset { get; private set; }

    public void UseMenuProfile() => Profile = UiMotionSettings.MenuProfile(_configuration);

    public UiMotionContainer()
    {
        IoCManager.InjectDependencies(this);
        _transition = new UiMotionTransition(_configuration);
        _capture = Capture;
    }

    /// <summary>Begin appearing, or reverse a still visible exit image.</summary>
    public void Show()
    {
        var opacity = 0f;
        var offset = -Profile.Travel;
        if (_exit is { IsInsideTree: true, Visible: true })
        {
            opacity = _exit.Opacity;
            offset = _exit.Offset;
        }
        _exit?.Orphan();
        _exit = null;
        ReleaseTexture();
        _transition.Start(Profile, true, opacity, offset);
        InvalidateArrange();
    }

    /// <summary>
    /// Transfer the last frame to an inert exit image. The caller owns its UI parent and cancellation.
    /// No image is produced if the subtree never rendered or motion is disabled.
    /// </summary>
    public UiMotionSnapshot? CreateExitVisual()
    {
        if (_texture == null || !_configuration.GetCVar(UiMotionSettings.Enabled)
                             || !_configuration.GetCVar(UiMotionSettings.AnimateClosing) || Root == null)
        {
            ReleaseTexture();
            return null;
        }

        _exit?.Orphan();
        _exit = new UiMotionSnapshot(_texture, _configuration, Profile, GlobalPosition,
            _texture.Size / UIScale, _renderedOpacity, _renderedOffset, _renderedTint,
            _shader ??= _prototypes.Index(CompositeShader).Instance());
        _texture = null;
        Root.ModalRoot.AddChild(_exit);
        return _exit;
    }

    /// <summary>Opacity for an item in an opt-in sequence, sharing this host's timing and preferences.</summary>
    public float GetStaggeredOpacity(int index, int count)
    {
        return _transition.GetStaggeredOpacity(index, count);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        var offset = _transition.Offset;
        _transition.Advance(args.DeltaSeconds);
        if (offset != _transition.Offset)
            InvalidateArrange();
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var offset = _transition.Offset;
        if (Root != null)
            offset = Math.Clamp(offset, -GlobalPosition.Y, Math.Max(-GlobalPosition.Y, Root.Height - GlobalPosition.Y - finalSize.Y));
        VisualOffset = new Vector2(0, offset);
        foreach (var child in Children)
            child.Arrange(UIBox2.FromDimensions(VisualOffset, finalSize));
        return finalSize;
    }

    protected override void RenderChildOverride(ref ControlRenderArguments args, int childIndex, Vector2i position)
    {
        if (!_configuration.GetCVar(UiMotionSettings.Enabled) || !_configuration.GetCVar(UiMotionSettings.AnimateClosing))
        {
            ReleaseTexture();
            var original = args.Modulate;
            args.Modulate = original.WithAlpha(original.A * _transition.Opacity);
            base.RenderChildOverride(ref args, childIndex, position);
            args.Modulate = original;
            return;
        }

        var child = GetChild(childIndex);
        if (child.PixelWidth <= 0 || child.PixelHeight <= 0)
            return;

        if (_texture?.Size != child.PixelSize)
        {
            ReleaseTexture();
            _texture = _clyde.CreateRenderTarget(child.PixelSize, RenderTargetColorFormat.Rgba8Srgb,
                name: nameof(UiMotionContainer));
        }

        _renderHandle = args.Handle;
        _renderChild = child;
        args.Handle.RenderInRenderTarget(_texture, _capture, Color.Transparent);
        _renderHandle = null;
        _renderChild = null;

        var handle = args.Handle.DrawingHandleScreen;
        var transform = handle.GetTransform();
        var modulate = handle.Modulate;
        handle.SetTransform(Matrix3x2.Identity);
        UiMotionSnapshot.DrawImage(handle, _texture.Texture, UIBox2.FromDimensions(position, child.PixelSize),
            _shader ??= _prototypes.Index(CompositeShader).Instance(), args.Modulate, _transition.Opacity);
        handle.Modulate = modulate;
        handle.SetTransform(transform);
        _renderedOpacity = _transition.Opacity;
        _renderedOffset = VisualOffset.Y;
        _renderedTint = args.Modulate;
    }

    private void Capture()
    {
        if (_renderHandle == null || _renderChild == null)
            return;

        _renderHandle.SetScissor(null);
        _renderHandle.DrawingHandleScreen.SetTransform(Matrix3x2.Identity);
        UserInterfaceManager.RenderControl(_renderHandle, _renderChild, Vector2i.Zero);
    }

    protected override void ExitedTree()
    {
        ReleaseTexture();
        base.ExitedTree();
    }

    protected override void VisibilityChanged(bool newVisible)
    {
        if (!newVisible)
            ReleaseTexture();
        base.VisibilityChanged(newVisible);
    }

    private void ReleaseTexture()
    {
        _texture?.Dispose();
        _texture = null;
    }
}
// Arcane-End
