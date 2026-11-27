// Arcane-Start
using System.Linq;
using System.Text.Json;
using Content.Arcane.Common.Preferences;

namespace Content.Server.Database;

/// <summary>
/// Stores <see cref="CharacterCustomization"/> in the profile table. The hair gradient goes into one JSON column.
/// </summary>
public static class CharacterCustomizationDb
{
    private sealed class GradientData
    {
        public List<string> Colors { get; set; } = [];
        public HairGradientStyle Style { get; set; }
        public float Offset { get; set; } = 0.5f;
    }

    public static void Write(Profile profile, CharacterCustomization customization)
    {
        profile.CustomSpeciesName = customization.CustomSpeciesName;
        profile.HairGradientEnabled = customization.HairGradientEnabled;
        profile.HairGradientData = JsonSerializer.Serialize(new GradientData
        {
            Colors = customization.HairGradientColors.Select(c => c.ToHex()).ToList(),
            Style = customization.HairGradientStyle,
            Offset = customization.HairGradientOffset,
        });
    }

    public static CharacterCustomization Read(Profile profile)
    {
        var customization = new CharacterCustomization
        {
            CustomSpeciesName = profile.CustomSpeciesName ?? string.Empty,
            HairGradientEnabled = profile.HairGradientEnabled,
        };

        if (string.IsNullOrEmpty(profile.HairGradientData))
            return customization;

        try
        {
            var data = JsonSerializer.Deserialize<GradientData>(profile.HairGradientData);
            if (data == null)
                return customization;

            var colors = new List<Color>();
            foreach (var hex in data.Colors)
            {
                if (Color.TryFromHex(hex.Trim(), out var color))
                    colors.Add(color);
            }

            if (colors.Count > 0)
                customization.HairGradientColors = colors;
            customization.HairGradientStyle = data.Style;
            customization.HairGradientOffset = data.Offset;
        }
        catch (JsonException)
        {
            customization.HairGradientEnabled = false;
        }

        return customization;
    }
}
// Arcane-End
