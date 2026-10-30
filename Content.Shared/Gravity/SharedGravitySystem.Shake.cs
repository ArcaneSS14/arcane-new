namespace Content.Shared.Gravity;

public abstract partial class SharedGravitySystem
{
    [Dependency] private EntityQuery<GravityComponent> _gravityQuery = default!;

    protected const float GravityKick = 100.0f;
    protected const float ShakeCooldown = 0.2f;

    private void UpdateShake()
    {
        var curTime = Timing.CurTime;
        var query = EntityQueryEnumerator<GravityShakeComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.NextShake <= curTime)
            {
                if (comp.ShakeTimes == 0 || !_gravityQuery.TryGetComponent(uid, out var gravity))
                {
                    RemCompDeferred<GravityShakeComponent>(uid);
                    continue;
                }

                ShakeGrid(uid, gravity);
                comp.ShakeTimes--;
                comp.NextShake += TimeSpan.FromSeconds(ShakeCooldown);
                Dirty(uid, comp);
            }
        }
    }

    public void StartGridShake(EntityUid uid, GravityComponent? gravity = null)
    {
        if (Terminating(uid))
            return;

        if (!Resolve(uid, ref gravity, false))
            return;

        // Arcane-Edit-Start
        if (!TryComp<GravityShakeComponent>(uid, out var shake))
        {
            shake = AddComp<GravityShakeComponent>(uid);
            shake.NextShake = Timing.CurTime;
        }

        if (TryArcaneScreenshakeGrid(uid))
        {
            shake.ShakeTimes = 0;
            Dirty(uid, shake);
            return;
        }

        shake.ShakeTimes = 10;
        Dirty(uid, shake);
        // Arcane-Edit-End
    }

    protected virtual void ShakeGrid(EntityUid uid, GravityComponent? comp = null) {}
}
