// Arcane-Start
using System.Numerics;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Content.Arcane.Common.CCVar;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Chat.UI;

[CommonSheetlet]
public sealed class RunechatSheetlet : Sheetlet<NanotrasenStylesheet>
{
    public const string FontPath = "/Fonts/_Arcane/Runechat/Grand9kPixelUnicode.ttf";
    public const string PressStart2PFontPath = "/Fonts/_Arcane/Runechat/PressStart2P-Regular.ttf";
    public const string Tiny5FontPath = "/Fonts/_Arcane/Runechat/Tiny5-Regular.ttf";
    public const string Grand9kFontId = "grand9k";
    public const string PressStart2PFontId = "press-start-2p";
    public const string Tiny5FontId = "tiny5";
    public const float MinimumFontSize = 6f;
    public const float MaximumFontSize = 32f;
    public const float MinimumTypingSpeed = 1f;
    public const float MaximumTypingSpeed = 120f;
    public const float MinimumPostTypingLifetime = 0.25f;
    public const float MaximumPostTypingLifetime = 15f;

    public static string NormalizeFontId(string? value)
    {
        return value switch
        {
            Grand9kFontId or PressStart2PFontId or Tiny5FontId => value,
            _ => Grand9kFontId,
        };
    }

    public static string GetFontPath(string? fontId)
    {
        return NormalizeFontId(fontId) switch
        {
            PressStart2PFontId => PressStart2PFontPath,
            Tiny5FontId => Tiny5FontPath,
            _ => FontPath,
        };
    }

    public static string GetSelectedFontPath()
    {
        var configuration = IoCManager.Resolve<IConfigurationManager>();
        return GetFontPath(configuration.GetCVar(ArcaneCVars.ChatRunechatFont));
    }

    public static string GetSelectedWhisperFontPath()
    {
        var configuration = IoCManager.Resolve<IConfigurationManager>();
        return GetFontPath(configuration.GetCVar(ArcaneCVars.ChatRunechatWhisperFont));
    }

    public static int GetNormalFontSize()
    {
        var configuration = IoCManager.Resolve<IConfigurationManager>();
        return ToPointSize(NormalizeFontSize(configuration.GetCVar(ArcaneCVars.ChatRunechatFontSize), 12f));
    }

    public static int GetLoudFontSize()
    {
        return (int) MathF.Round(GetNormalFontSize() * 1.5f);
    }

    public static int GetWhisperFontSize()
    {
        var configuration = IoCManager.Resolve<IConfigurationManager>();
        return ToPointSize(NormalizeFontSize(configuration.GetCVar(ArcaneCVars.ChatRunechatWhisperFontSize), 9f));
    }

    public static float NormalizeFontSize(float value, float defaultValue)
    {
        return NormalizeFloat(value, defaultValue, MinimumFontSize, MaximumFontSize);
    }

    public static float GetTypingSpeed()
    {
        var configuration = IoCManager.Resolve<IConfigurationManager>();
        return NormalizeFloat(configuration.GetCVar(ArcaneCVars.ChatRunechatTypingSpeed), 30f, MinimumTypingSpeed, MaximumTypingSpeed);
    }

    public static float GetPostTypingLifetime()
    {
        var configuration = IoCManager.Resolve<IConfigurationManager>();
        return NormalizeFloat(configuration.GetCVar(ArcaneCVars.ChatRunechatPostTypingLifetime), 2f, MinimumPostTypingLifetime, MaximumPostTypingLifetime);
    }

    public static bool IsTypingAnimationEnabled()
    {
        return IoCManager.Resolve<IConfigurationManager>().GetCVar(ArcaneCVars.ChatRunechatTypingAnimation);
    }

    public static bool IsOutlineEnabled()
    {
        return IoCManager.Resolve<IConfigurationManager>().GetCVar(ArcaneCVars.ChatRunechatOutline);
    }

    public static Color GetTextColor()
    {
        var configuration = IoCManager.Resolve<IConfigurationManager>();
        if (configuration.GetCVar(ArcaneCVars.ChatRunechatRandomColor))
        {
            return Color.FromHsl(new Vector4(Random.Shared.NextSingle(), 0.65f, 0.7f, 1f));
        }

        return NormalizeTextColor(configuration.GetCVar(ArcaneCVars.ChatRunechatColor));
    }

    public static Color NormalizeTextColor(string? value)
    {
        return value != null && Color.TryFromHex(value, out var color) ? color : Color.White;
    }

    public static float NormalizeFloat(float value, float defaultValue, float minimum, float maximum)
    {
        return float.IsFinite(value) && value >= minimum && value <= maximum
            ? value
            : defaultValue;
    }

    public static Stylesheet CreateFontStylesheet(FontResource font, int size)
    {
        return new Stylesheet(
        [
            E<RichTextLabel>().Prop(Label.StylePropertyFont, new VectorFont(font, size)),
        ]);
    }

    public static Stylesheet CreateLabelFontStylesheet(FontResource font, int size)
    {
        return new Stylesheet(
        [
            E<Label>().Prop(Label.StylePropertyFont, new VectorFont(font, size)),
        ]);
    }

    private static int ToPointSize(float value)
    {
        return (int) MathF.Round(value);
    }

    public override StyleRule[] GetRules(NanotrasenStylesheet sheet, object config)
    {
        var font = ResCache.GetResource<FontResource>(GetSelectedFontPath());
        var whisperFont = ResCache.GetResource<FontResource>(GetSelectedWhisperFontPath());
        var normalSize = GetNormalFontSize();
        var loudSize = GetLoudFontSize();
        var whisperSize = GetWhisperFontSize();

        return
        [
            E<RichTextLabel>()
                .Class(RunechatSpeechBubble.StyleClassNormal)
                .Prop(Label.StylePropertyFont, new VectorFont(font, normalSize)),
            E<RichTextLabel>()
                .Class(RunechatSpeechBubble.StyleClassLoud)
                .Prop(Label.StylePropertyFont, new VectorFont(font, loudSize)),
            E<RichTextLabel>()
                .Class(RunechatSpeechBubble.StyleClassWhisper)
                .Prop(Label.StylePropertyFont, new VectorFont(whisperFont, whisperSize)),
        ];
    }
}
// Arcane-End
