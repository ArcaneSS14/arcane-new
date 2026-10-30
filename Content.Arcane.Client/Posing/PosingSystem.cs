using Content.Arcane.Common.Input;
using Content.Arcane.Shared.Posing;
using Robust.Client.Input;
using Robust.Client.Player;

namespace Content.Arcane.Client.Posing;

public sealed partial class PosingSystem : SharedPosingSystem
{
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private const string PosingContext = "posing";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PosingComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);

        _input.Contexts.GetContext("human").AddFunction(ArcaneKeyFunctions.TogglePosing);

        var posing = _input.Contexts.New(PosingContext, "common");
        posing.AddFunction(ArcaneKeyFunctions.TogglePosing);
        posing.AddFunction(ArcaneKeyFunctions.PosingOffsetUp);
        posing.AddFunction(ArcaneKeyFunctions.PosingOffsetDown);
        posing.AddFunction(ArcaneKeyFunctions.PosingOffsetLeft);
        posing.AddFunction(ArcaneKeyFunctions.PosingOffsetRight);
        posing.AddFunction(ArcaneKeyFunctions.PosingRotatePositive);
        posing.AddFunction(ArcaneKeyFunctions.PosingRotateNegative);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _input.Contexts.Remove(PosingContext);
    }

    private void OnAfterHandleState(Entity<PosingComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_player.LocalEntity == ent.Owner)
        {
            // The server can end posing (stun, death), so keep the input context in sync.
            var context = ent.Comp.Posing ? PosingContext : ent.Comp.DefaultInputContext;
            if (_input.Contexts.ActiveContext.Name != context)
                _input.Contexts.SetActiveContext(context);
        }

        if (!ent.Comp.Posing)
            ResetSprite(ent);
    }

    protected override void OnPosingToggled(EntityUid uid, PosingComponent posing)
    {
        base.OnPosingToggled(uid, posing);

        if (_player.LocalEntity == uid)
            _input.Contexts.SetActiveContext(posing.Posing ? PosingContext : posing.DefaultInputContext);

        ResetSprite((uid, posing));
    }

    private void ResetSprite(Entity<PosingComponent> ent)
    {
        _sprite.SetOffset(ent.Owner, ent.Comp.DefaultOffset);
        _sprite.SetRotation(ent.Owner, Angle.FromDegrees(ent.Comp.DefaultAngle));
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<PosingComponent>();
        while (query.MoveNext(out var uid, out var posing))
        {
            if (!posing.Posing)
                continue;

            _sprite.SetOffset(uid, posing.DefaultOffset + posing.CurrentOffset);
            _sprite.SetRotation(uid, posing.CurrentAngle);
        }
    }
}
