// Arcane-Start
using System.Numerics;
using System.Linq;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Noise;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.Camera;

public sealed partial class ArcaneScreenshakeSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private INetManager _net = default!;

    private bool _reducedMotion;
    private float _intensity = 1f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ArcaneScreenshakeComponent, ArcaneGetEyeRotationEvent>(OnGetRotation);
        SubscribeLocalEvent<ArcaneScreenshakeComponent, GetEyeOffsetEvent>(OnGetOffset);
        SubscribeLocalEvent<ArcaneScreenshakeComponent, EntityUnpausedEvent>(OnUnpaused);

        if (_net.IsClient)
        {
            Subs.CVar(_configuration, CCVars.ReducedMotion, value => _reducedMotion = value, true);
            Subs.CVar(_configuration, CCVars.ScreenShakeIntensity, value => _intensity = value, true);
        }
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<ArcaneScreenshakeComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            var commandsChanged = false;
            for (var i = component.Commands.Count - 1; i >= 0; i--)
            {
                if (_timing.CurTime >= component.Commands[i].End)
                {
                    component.Commands.RemoveAt(i);
                    commandsChanged = true;
                }
            }

            if (component.Commands.Count == 0)
                RemCompDeferred<ArcaneScreenshakeComponent>(uid);
            else if (commandsChanged)
                Dirty(uid, component);
        }
    }

    private void OnGetOffset(Entity<ArcaneScreenshakeComponent> entity, ref GetEyeOffsetEvent args)
    {
        if (_reducedMotion || _intensity <= 0f || !HasComp<EyeComponent>(entity))
            return;

        var noise = new FastNoiseLite(67);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        var offset = Vector2.Zero;
        foreach (var command in entity.Comp.Commands)
        {
            if (command.Translation is not { } parameters)
                continue;

            var trauma = CalculateTrauma(parameters, command.Start);
            if (trauma <= 0f)
                continue;

            noise.SetFrequency(parameters.Frequency);
            var time = (float) _timing.RealTime.TotalMilliseconds;
            var start = (float) command.Start.TotalMilliseconds;
            var x = 0.15f * trauma * noise.GetNoise(time, start);
            noise.SetSeed(68);
            var y = 0.15f * trauma * noise.GetNoise(time, start);
            noise.SetSeed(67);
            offset += new Vector2(x, y);
        }

        args.Offset += offset * _intensity;
    }

    private void OnGetRotation(Entity<ArcaneScreenshakeComponent> entity, ref ArcaneGetEyeRotationEvent args)
    {
        if (_reducedMotion || _intensity <= 0f || !HasComp<EyeComponent>(entity))
            return;

        var noise = new FastNoiseLite(487);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        var rotation = Angle.Zero;
        foreach (var command in entity.Comp.Commands)
        {
            if (command.Rotation is not { } parameters)
                continue;

            var trauma = CalculateTrauma(parameters, command.Start);
            if (trauma <= 0f)
                continue;

            noise.SetFrequency(parameters.Frequency);
            rotation += Angle.FromDegrees(20f * trauma * noise.GetNoise((float) _timing.RealTime.TotalMilliseconds, (float) command.Start.TotalMilliseconds));
        }

        args.Rotation += rotation * _intensity;
    }

    private void OnUnpaused(Entity<ArcaneScreenshakeComponent> entity, ref EntityUnpausedEvent args)
    {
        var pausedTime = args.PausedTime;
        entity.Comp.Commands = entity.Comp.Commands.Select(command => command with
        {
            Start = command.Start + pausedTime,
            End = command.End + pausedTime,
        }).ToList();
        Dirty(entity);
    }

    private float CalculateTrauma(ArcaneScreenshakeParameters parameters, TimeSpan start)
    {
        var seconds = (float) (_timing.CurTime - start).TotalSeconds;
        return seconds < 0f ? 0f : -seconds * seconds * parameters.DecayRate + parameters.Trauma;
    }

    public void Screenshake(EntityUid uid, ArcaneScreenshakeParameters? translation, ArcaneScreenshakeParameters? rotation)
    {
        if (!HasComp<EyeComponent>(uid))
            return;

        var start = _timing.CurTime;
        var translationEnd = translation is null ? 0f : MathF.Sqrt(translation.Trauma / translation.DecayRate);
        var rotationEnd = rotation is null ? 0f : MathF.Sqrt(rotation.Trauma / rotation.DecayRate);
        var component = EnsureComp<ArcaneScreenshakeComponent>(uid);
        component.Commands.Add(new ArcaneScreenshakeCommand(translation, rotation, start, start + TimeSpan.FromSeconds(MathF.Max(translationEnd, rotationEnd))));
        Dirty(uid, component);
    }

    public void Screenshake(Filter filter, ArcaneScreenshakeParameters? translation, ArcaneScreenshakeParameters? rotation)
    {
        foreach (var session in filter.Recipients)
        {
            if (session.AttachedEntity is { } uid)
                Screenshake(uid, translation, rotation);
        }
    }
}
// Arcane-End
