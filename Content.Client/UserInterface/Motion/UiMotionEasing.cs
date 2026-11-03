// Arcane-Start
namespace Content.Client.UserInterface.Motion;

/// <summary>
/// Material 3 Expressive motion tokens adapted to deterministic content-side UI transitions.
/// Timed transitions use Google's spring-to-curve values; interactive motion uses the spring tokens directly.
/// </summary>
public static class UiMotionEasing
{
    public const float FastSpatialSeconds = 0.35f;
    public const float DefaultSpatialSeconds = 0.5f;
    public const float SlowSpatialSeconds = 0.65f;
    public const float FastEffectsSeconds = 0.15f;
    public const float DefaultEffectsSeconds = 0.2f;
    public const float SlowEffectsSeconds = 0.3f;

    private static readonly CubicBezier ExpressiveFastSpatial = new(0.42f, 1.67f, 0.21f, 0.9f);
    private static readonly CubicBezier ExpressiveDefaultSpatial = new(0.38f, 1.21f, 0.22f, 1f);
    private static readonly CubicBezier ExpressiveSlowSpatial = new(0.39f, 1.29f, 0.35f, 0.98f);
    private static readonly CubicBezier StandardSpatial = new(0.27f, 1.06f, 0.18f, 1f);
    private static readonly CubicBezier FastEffects = new(0.31f, 0.94f, 0.34f, 1f);
    private static readonly CubicBezier DefaultEffects = new(0.34f, 0.8f, 0.34f, 1f);
    private static readonly CubicBezier SlowEffects = new(0.34f, 0.88f, 0.34f, 1f);

    public static float Spatial(float progress, bool expressive, float durationSeconds)
    {
        if (!expressive)
            return StandardSpatial.Sample(progress);

        return durationSeconds <= FastSpatialSeconds
            ? ExpressiveFastSpatial.Sample(progress)
            : durationSeconds <= DefaultSpatialSeconds
                ? ExpressiveDefaultSpatial.Sample(progress)
                : ExpressiveSlowSpatial.Sample(progress);
    }

    public static float Effects(float progress, float durationSeconds)
    {
        return durationSeconds <= FastEffectsSeconds
            ? FastEffects.Sample(progress)
            : durationSeconds <= DefaultEffectsSeconds
                ? DefaultEffects.Sample(progress)
                : SlowEffects.Sample(progress);
    }

    private readonly record struct CubicBezier(float X1, float Y1, float X2, float Y2)
    {
        public float Sample(float progress)
        {
            progress = Math.Clamp(progress, 0, 1);
            if (progress is 0 or 1)
                return progress;

            var parameter = progress;
            for (var i = 0; i < 6; i++)
            {
                var error = Coordinate(parameter, X1, X2) - progress;
                var derivative = Derivative(parameter, X1, X2);
                if (MathF.Abs(error) < 0.00001f || MathF.Abs(derivative) < 0.00001f)
                    break;
                parameter = Math.Clamp(parameter - error / derivative, 0, 1);
            }

            // Newton iteration can settle at an endpoint for strongly overshooting curves.
            // Bisection gives a deterministic answer for every valid CSS cubic-bezier x pair.
            var lower = 0f;
            var upper = 1f;
            for (var i = 0; i < 12; i++)
            {
                var x = Coordinate(parameter, X1, X2);
                if (MathF.Abs(x - progress) < 0.00001f)
                    break;
                if (x < progress)
                    lower = parameter;
                else
                    upper = parameter;
                parameter = (lower + upper) * 0.5f;
            }

            return Coordinate(parameter, Y1, Y2);
        }

        private static float Coordinate(float t, float first, float second)
        {
            var inverse = 1 - t;
            return 3 * inverse * inverse * t * first + 3 * inverse * t * t * second + t * t * t;
        }

        private static float Derivative(float t, float first, float second)
        {
            var inverse = 1 - t;
            return 3 * inverse * inverse * first
                   + 6 * inverse * t * (second - first)
                   + 3 * t * t * (1 - second);
        }
    }
}

/// <summary>Velocity-preserving Material 3 spring for hover and other interruptible state changes.</summary>
public sealed class UiMotionSpring
{
    public float Value { get; private set; }
    public float Velocity { get; private set; }

    public float Advance(float target, float deltaSeconds, float dampingRatio = 0.6f, float stiffness = 800f)
    {
        if (!float.IsFinite(target) || !float.IsFinite(deltaSeconds) || deltaSeconds <= 0)
            return Value;

        dampingRatio = Math.Clamp(float.IsFinite(dampingRatio) ? dampingRatio : 1, 0.01f, 1f);
        stiffness = Math.Max(0.01f, float.IsFinite(stiffness) ? stiffness : 800);
        var frequency = MathF.Sqrt(stiffness);
        var displacement = Value - target;

        if (dampingRatio < 0.999f)
        {
            var dampedFrequency = frequency * MathF.Sqrt(1 - dampingRatio * dampingRatio);
            var decay = MathF.Exp(-dampingRatio * frequency * deltaSeconds);
            var cosine = MathF.Cos(dampedFrequency * deltaSeconds);
            var sine = MathF.Sin(dampedFrequency * deltaSeconds);
            var coefficient = (Velocity + dampingRatio * frequency * displacement) / dampedFrequency;
            var nextDisplacement = decay * (displacement * cosine + coefficient * sine);
            Velocity = decay * (Velocity * cosine
                - (dampingRatio * frequency * Velocity + frequency * frequency * displacement) / dampedFrequency * sine);
            Value = target + nextDisplacement;
        }
        else
        {
            var coefficient = Velocity + frequency * displacement;
            var decay = MathF.Exp(-frequency * deltaSeconds);
            var nextDisplacement = (displacement + coefficient * deltaSeconds) * decay;
            Velocity = (Velocity - frequency * coefficient * deltaSeconds) * decay;
            Value = target + nextDisplacement;
        }

        if (MathF.Abs(Value - target) < 0.0005f && MathF.Abs(Velocity) < 0.005f)
            Snap(target);
        return Value;
    }

    public float Snap(float target)
    {
        Value = float.IsFinite(target) ? target : 0;
        Velocity = 0;
        return Value;
    }
}
// Arcane-End
