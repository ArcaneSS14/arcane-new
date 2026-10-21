using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Arcane.Client.Medical.Overlays;

/// <summary>
/// Dark critical vision followed by the white passage-to-death mask, above other medical effects.
/// </summary>
public sealed partial class CriticalVisionOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> Shader = "GradientCircleMask";
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;

    private readonly ShaderInstance _darkShader;
    private readonly ShaderInstance _whiteShader;
    private float _darkness;
    private float _white;
    private EntityUid? _lastPatient;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;
    public EntityUid? Patient { get; set; }
    public float Darkness { get; set; }
    public float WhiteLevel { get; set; }

    public CriticalVisionOverlay()
    {
        IoCManager.InjectDependencies(this);
        var prototype = _prototypes.Index(Shader);
        _darkShader = prototype.InstanceUnique();
        _whiteShader = prototype.InstanceUnique();
        _darkShader.SetParameter("time", 0f);
        _darkShader.SetParameter("color", Vector3.Zero);
        _whiteShader.SetParameter("time", 0f);
        _whiteShader.SetParameter("color", Vector3.One);
        ZIndex = 14;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity != Patient)
        {
            _lastPatient = null;
            return false;
        }

        if (!_entities.TryGetComponent(_player.LocalEntity, out EyeComponent? eye) || args.Viewport.Eye != eye.Eye)
            return false;

        if (_lastPatient != Patient)
        {
            _darkness = _white = 0f;
            _lastPatient = Patient;
        }

        var step = Math.Clamp((float) _timing.FrameTime.TotalSeconds * 5f, 0f, 1f);
        _darkness = Approach(_darkness, Darkness, step);
        _white = Approach(_white, WhiteLevel, step);
        return _darkness > 0f || _white > 0f;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var width = (float) args.ViewportBounds.Width;
        var handle = args.WorldHandle;
        if (_darkness > 0f)
        {
            // Keep the narrow opening; occasional easing resembles TG's flickering unconscious view.
            var time = (float) _timing.RealTime.TotalSeconds;
            var blink = MathF.Max(0f, MathF.Sin(time * 2f) + 2f * MathF.Sin(time) + MathF.Sin(time * 0.5f) - 3f);
            SetMask(_darkShader, width * 0.02f, width * (0.6f - _darkness * 0.54f), 1f - blink / 1.5f);
            handle.UseShader(_darkShader);
            handle.DrawRect(args.WorldBounds, Color.White);
        }

        if (_white > 0f)
        {
            SetMask(_whiteShader, width * (0.6f - _white * 0.58f), width * (2f - _white), _white);
            handle.UseShader(_whiteShader);
            handle.DrawRect(args.WorldBounds, Color.White);
        }
        handle.UseShader(null);
    }

    private static float Approach(float current, float target, float step)
        => MathF.Abs(current - target) < 0.001f ? target : current + (target - current) * step;

    private static void SetMask(ShaderInstance shader, float inner, float outer, float opacity)
    {
        shader.SetParameter("darknessAlphaOuter", opacity);
        shader.SetParameter("innerCircleRadius", inner);
        shader.SetParameter("outerCircleRadius", outer);
    }

    protected override void DisposeBehavior()
    {
        _darkShader.Dispose();
        _whiteShader.Dispose();
        base.DisposeBehavior();
    }
}
