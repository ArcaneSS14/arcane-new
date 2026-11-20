using System.Linq;
using System.Text.RegularExpressions;
using Robust.Shared.Serialization;

namespace Content.Arcane.Common.Preferences;

/// <summary>
/// How a hair gradient blends the roots colour into the tips colour.
/// </summary>
[Serializable, NetSerializable]
public enum HairGradientStyle : byte
{
    Ombre = 0,
    Split = 1,
    Underdye = 2,
    Streaks = 3,
}

/// <summary>
/// Arcane character customization stored in the profile: a custom species name, ears drawn above the hair and a
/// hair gradient. Treated as immutable once it is stored in a profile: edits go through <see cref="Clone"/>.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class CharacterCustomization : IEquatable<CharacterCustomization>
{
    private static readonly Regex RestrictedSpeciesNameRegex = new(@"[^А-Яа-яёЁA-Za-z0-9' -]|\B\s+|\s+\B", RegexOptions.Compiled);

    /// <summary>
    /// Shown instead of the species name wherever the species is displayed, for species that allow it.
    /// </summary>
    [DataField]
    public string CustomSpeciesName = string.Empty;

    [DataField]
    public bool HairGradientEnabled;

    /// <summary>
    /// Roots colour, then tips colour.
    /// </summary>
    [DataField]
    // Not a collection expression: for a List it compiles to CollectionsMarshal.SetCount, which the client sandbox forbids.
    public List<Color> HairGradientColors = new() { Color.Black, Color.Black };

    [DataField]
    public HairGradientStyle HairGradientStyle = HairGradientStyle.Ombre;

    /// <summary>
    /// Where the transition sits, 0 to 1.
    /// </summary>
    [DataField]
    public float HairGradientOffset = 0.5f;

    public CharacterCustomization Clone()
    {
        var copy = (CharacterCustomization) MemberwiseClone();
        copy.HairGradientColors = new List<Color>(HairGradientColors);
        return copy;
    }

    /// <summary>
    /// "Custom (Species)" when a custom name is set, otherwise the species name.
    /// </summary>
    public static string GetSpeciesLongName(string species, string? customSpeciesName)
    {
        return string.IsNullOrWhiteSpace(customSpeciesName)
            ? species
            : $"{customSpeciesName} ({species})";
    }

    /// <summary>
    /// Copy with the custom species name cleaned up (empty when the species does not allow one or it names another
    /// species) and the gradient values clamped.
    /// </summary>
    /// <param name="allowsCustomName">The character's species allows a custom name.</param>
    /// <param name="maxNameLength">Longest allowed custom species name.</param>
    /// <param name="forbiddenNames">Localized names of every species, when they are restricted.</param>
    public CharacterCustomization Validated(bool allowsCustomName, int maxNameLength, IEnumerable<string>? forbiddenNames)
    {
        var copy = Clone();
        var name = allowsCustomName ? CustomSpeciesName ?? string.Empty : string.Empty;
        if (name.Length > maxNameLength)
            name = name[..maxNameLength];

        name = RestrictedSpeciesNameRegex.Replace(name, string.Empty).Trim();
        if (forbiddenNames != null && forbiddenNames.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
            name = string.Empty;

        copy.CustomSpeciesName = name;

        var colors = HairGradientColors.Take(2).Select(c => c.WithAlpha(1f)).ToList();
        while (colors.Count < 2)
            colors.Add(colors.Count > 0 ? colors[0] : Color.Black);

        copy.HairGradientColors = colors;
        copy.HairGradientStyle = Enum.IsDefined(HairGradientStyle) ? HairGradientStyle : HairGradientStyle.Ombre;
        copy.HairGradientOffset = float.IsFinite(HairGradientOffset) ? Math.Clamp(HairGradientOffset, 0f, 1f) : 0.5f;
        return copy;
    }

    public bool Equals(CharacterCustomization? other)
    {
        if (other is null)
            return false;

        return CustomSpeciesName == other.CustomSpeciesName &&
               HairGradientEnabled == other.HairGradientEnabled &&
               HairGradientColors.SequenceEqual(other.HairGradientColors) &&
               HairGradientStyle == other.HairGradientStyle &&
               MathF.Abs(HairGradientOffset - other.HairGradientOffset) < 0.001f;
    }

    public override bool Equals(object? obj)
    {
        return obj is CharacterCustomization other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(CustomSpeciesName);
        hash.Add(HairGradientEnabled);
        foreach (var color in HairGradientColors)
            hash.Add(color);
        hash.Add(HairGradientStyle);
        hash.Add(MathF.Round(HairGradientOffset, 3));
        return hash.ToHashCode();
    }
}
