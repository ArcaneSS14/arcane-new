// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Text.RegularExpressions;
using Content.Arcane.Common.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Serialization;

namespace Content.Arcane.Common.Preferences;

/// <summary>
/// Additional character description sections, shown in the detail examine window.
/// Treated as immutable once it is stored in a profile: edits go through <see cref="Clone"/>.
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class ExtendedFlavorText : IEquatable<ExtendedFlavorText>
{
    private static readonly Regex TagRegex = new(@"\[/?(?<tag>[a-zA-Z]+)[^\]]*\]", RegexOptions.Compiled);
    private static readonly string[] AllowedTags = ["bold", "italic", "bolditalic", "color"];
    private static readonly char[] ListSeparators = [',', ' ', '\n', '\r', '\t', ';'];

    [DataField]
    public string Character = string.Empty;

    [DataField]
    public string Ooc = string.Empty;

    [DataField]
    public string Tags = string.Empty;

    [DataField]
    public string Links = string.Empty;

    [DataField]
    public string Green = string.Empty;

    [DataField]
    public string Yellow = string.Empty;

    [DataField]
    public string Red = string.Empty;

    [DataField]
    public string Nsfw = string.Empty;

    [DataField]
    public string NsfwOoc = string.Empty;

    [DataField]
    public string NsfwLinks = string.Empty;

    [DataField]
    public string NsfwTags = string.Empty;

    public bool IsEmpty => Equals(Empty);

    private static readonly ExtendedFlavorText Empty = new();

    public ExtendedFlavorText Clone()
    {
        return (ExtendedFlavorText) MemberwiseClone();
    }

    /// <summary>
    /// Copy without the NSFW sections, for viewers who must not receive them.
    /// </summary>
    public ExtendedFlavorText WithoutNsfw()
    {
        var copy = Clone();
        copy.Nsfw = string.Empty;
        copy.NsfwOoc = string.Empty;
        copy.NsfwLinks = string.Empty;
        copy.NsfwTags = string.Empty;
        return copy;
    }

    /// <summary>
    /// Copy with every section cut to its configured length, markup reduced to safe tags and tags normalized.
    /// </summary>
    public ExtendedFlavorText Validated(IConfigurationManager cfg)
    {
        var ooc = cfg.GetCVar(ArcaneCVars.FlavorOocLength);
        var preferences = cfg.GetCVar(ArcaneCVars.FlavorPreferencesLength);
        var nsfw = cfg.GetCVar(ArcaneCVars.FlavorNsfwLength);
        var tags = cfg.GetCVar(ArcaneCVars.FlavorTagsLength);
        var links = cfg.GetCVar(ArcaneCVars.FlavorLinksLength);

        return new ExtendedFlavorText
        {
            Character = ValidateText(Character, cfg.GetCVar(ArcaneCVars.FlavorCharacterLength)),
            Ooc = ValidateText(Ooc, ooc),
            Tags = FormatTags(Cut(Tags, tags)),
            Links = Cut(Links, links),
            Green = ValidateText(Green, preferences),
            Yellow = ValidateText(Yellow, preferences),
            Red = ValidateText(Red, preferences),
            Nsfw = ValidateText(Nsfw, nsfw),
            NsfwOoc = ValidateText(NsfwOoc, ooc),
            NsfwLinks = Cut(NsfwLinks, links),
            NsfwTags = FormatTags(Cut(NsfwTags, tags)),
        };
    }

    /// <summary>
    /// Cuts the text to <paramref name="maxLength"/> and strips every markup tag except basic formatting.
    /// </summary>
    public static string ValidateText(string text, int maxLength)
    {
        return SanitizeMarkup(Cut(text, maxLength));
    }

    /// <summary>
    /// Removes all markup tags except bold, italic and color, so player text can't change fonts, sizes or layout.
    /// </summary>
    public static string SanitizeMarkup(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return TagRegex.Replace(text, match =>
            AllowedTags.Contains(match.Groups["tag"].Value.ToLowerInvariant()) ? match.Value : string.Empty);
    }

    /// <summary>
    /// Splits a free-form list of links or tags into entries.
    /// </summary>
    public static string[] SplitList(string text)
    {
        return text.Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string Cut(string text, int maxLength)
    {
        return text.Length > maxLength ? text[..maxLength] : text;
    }

    private static string FormatTags(string tags)
    {
        var formatted = SplitList(tags)
            .Select(tag => tag.StartsWith('#') ? tag : "#" + tag)
            .Where(tag => tag.Length > 1);

        return string.Join(", ", formatted);
    }

    public bool Equals(ExtendedFlavorText? other)
    {
        if (other is null)
            return false;

        return Character == other.Character
               && Ooc == other.Ooc
               && Tags == other.Tags
               && Links == other.Links
               && Green == other.Green
               && Yellow == other.Yellow
               && Red == other.Red
               && Nsfw == other.Nsfw
               && NsfwOoc == other.NsfwOoc
               && NsfwLinks == other.NsfwLinks
               && NsfwTags == other.NsfwTags;
    }

    public override bool Equals(object? obj)
    {
        return obj is ExtendedFlavorText other && Equals(other);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Character);
        hash.Add(Ooc);
        hash.Add(Tags);
        hash.Add(Links);
        hash.Add(Green);
        hash.Add(Yellow);
        hash.Add(Red);
        hash.Add(Nsfw);
        hash.Add(NsfwOoc);
        hash.Add(NsfwLinks);
        hash.Add(NsfwTags);
        return hash.ToHashCode();
    }
}
