using System.Numerics;

namespace Content.Arcane.Shared.HeatTint;

public abstract partial class SharedHeatTintSystem : EntitySystem
{
    public static Color GetHeatColor(List<HeatTintPoint> gradient, float temperature)
    {
        if (gradient.Count == 0)
            return Color.White;
        if (gradient.Count == 1 || temperature <= gradient[0].Temperature)
            return gradient[0].Color;
        if (temperature >= gradient[^1].Temperature)
            return gradient[^1].Color;

        for (var i = 0; i < gradient.Count - 1; i++)
        {
            var lower = gradient[i];
            var upper = gradient[i + 1];
            if (temperature < lower.Temperature || temperature > upper.Temperature)
                continue;

            var range = upper.Temperature - lower.Temperature;
            var t = range > 0 ? (temperature - lower.Temperature) / range : 0f;
            return Color.FromLab(Vector4.Lerp(Color.ToLab(lower.Color), Color.ToLab(upper.Color), t));
        }

        return gradient[^1].Color;
    }
}
