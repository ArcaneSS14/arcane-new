// Arcane-Start
using System.Numerics;
using Content.Shared.Chat;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Client.ResourceManagement;

namespace Content.Client.Chat.UI;

/// <summary>
/// SS13-style outlined pixel text positioned above the speaker.
/// </summary>
public sealed class RunechatSpeechBubble : SpeechBubble
{
    public const string StyleClassNormal = "runechat";
    public const string StyleClassLoud = "runechatLoud";
    public const string StyleClassWhisper = "runechatWhisper";
    public const int NormalSize = 12;
    public const int LoudSize = 18;
    public const int WhisperSize = 9;

    private const float DesignPixelsPerEm = 8f;
    private const float RunechatMaxWidth = 200f;
    private static readonly TimeSpan FadeInTime = TimeSpan.FromSeconds(0.15f);
    private static readonly Color LoocColor = Color.FromHex("#48d1cc");

    private TimeSpan _spawnTime;
    private TimeSpan _lifetime;
    private IGameTiming? _timing;
    private RichTextLabel? _label;
    private string _visibleText = string.Empty;
    private int[] _graphemeStarts = [];
    private Color _textColor = Color.White;
    private float _typingSpeed;
    private bool _typingAnimation;
    private int _visibleGraphemes = -1;

    public RunechatSpeechBubble(ChatMessage message, EntityUid senderEntity, SpeechType type)
        : base(message, senderEntity, GetStyleKey(type))
    {
        SetLifetime(_lifetime);
    }

    private static string GetStyleKey(SpeechType type)
    {
        return type switch
        {
            SpeechType.Emote => "emote",
            SpeechType.Whisper => "whisper",
            SpeechType.Looc => "looc",
            _ => "say",
        };
    }

    protected override Control BuildBubble(ChatMessage message, string speechStyleClass, Color? fontColor = null)
    {
        _timing = IoCManager.Resolve<IGameTiming>();
        _spawnTime = _timing.RealTime;

        var type = speechStyleClass switch
        {
            "emote" => SpeechType.Emote,
            "whisper" => SpeechType.Whisper,
            "looc" => SpeechType.Looc,
            _ => SpeechType.Say,
        };

        var color = RunechatSheetlet.GetTextColor();
        var size = type == SpeechType.Whisper
            ? RunechatSheetlet.GetWhisperFontSize()
            : RunechatSheetlet.GetNormalFontSize();
        var isLoud = false;
        string text;

        switch (type)
        {
            case SpeechType.Emote:
                text = $"*{FormattedMessage.RemoveMarkupPermissive(message.Message).Trim()}*";
                break;
            case SpeechType.Looc:
                text = FormattedMessage.RemoveMarkupPermissive(message.Message);
                color = LoocColor;
                break;
            default:
                var content = SharedChatSystem.GetStringInsideTag(message, "BubbleContent");
                if (content.Length == 0)
                    content = message.Message;

                if (content.Contains("[bold]"))
                {
                    size = RunechatSheetlet.GetLoudFontSize();
                    isLoud = true;
                }

                text = FormattedMessage.RemoveMarkupPermissive(content);
                break;
        }

        _visibleText = text.Trim();
        _graphemeStarts = GetTextElementStarts(_visibleText);
        _textColor = color;
        _typingAnimation = RunechatSheetlet.IsTypingAnimationEnabled();
        _typingSpeed = RunechatSheetlet.GetTypingSpeed();
        var typingDuration = _typingAnimation
            ? TimeSpan.FromSeconds(_graphemeStarts.Length / _typingSpeed)
            : TimeSpan.Zero;
        _lifetime = typingDuration + TimeSpan.FromSeconds(RunechatSheetlet.GetPostTypingLifetime());

        var outline = GetDesignPixelSize(size);
        var padding = MathF.Ceiling(outline);
        var fontPath = type == SpeechType.Whisper
            ? RunechatSheetlet.GetSelectedWhisperFontPath()
            : RunechatSheetlet.GetSelectedFontPath();
        _label = new RichTextLabel
        {
            MaxWidth = RunechatMaxWidth,
            OutlineThicknessOverride = RunechatSheetlet.IsOutlineEnabled() ? outline : 0f,
            OutlineColorOverride = Color.Black,
            Margin = new Thickness(padding, padding, padding, padding + 2),
            Stylesheet = RunechatSheetlet.CreateFontStylesheet(
                IoCManager.Resolve<IResourceCache>().GetResource<FontResource>(fontPath),
                size),
            StyleClasses = { type == SpeechType.Whisper ? StyleClassWhisper : isLoud ? StyleClassLoud : StyleClassNormal },
        };
        SetVisibleText(_graphemeStarts.Length);
        return _label;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        UpdateTyping();
        base.FrameUpdate(args);

        var pixelPosition = (Position * UIScale).Rounded() / UIScale;
        LayoutContainer.SetPosition(this, pixelPosition);

        if (_timing == null)
            return;

        var age = (float) ((_timing.RealTime - _spawnTime).TotalSeconds / FadeInTime.TotalSeconds);
        if (age < 1f)
            Modulate = Modulate.WithAlpha(Modulate.A * Math.Clamp(age, 0f, 1f));
    }

    private static float GetDesignPixelSize(int pointSize)
    {
        return pointSize * 96f / 72f / DesignPixelsPerEm;
    }

    private void UpdateTyping()
    {
        if (!_typingAnimation || _timing == null)
        {
            return;
        }

        var elapsed = Math.Max(0f, (float) (_timing.RealTime - _spawnTime).TotalSeconds);
        var visible = Math.Min(_graphemeStarts.Length, (int) MathF.Floor(elapsed * _typingSpeed));
        SetVisibleText(visible);
    }

    private void SetVisibleText(int visibleGraphemes)
    {
        if (_label == null || visibleGraphemes == _visibleGraphemes)
            return;

        _visibleGraphemes = visibleGraphemes;
        var end = visibleGraphemes == _graphemeStarts.Length
            ? _visibleText.Length
            : _graphemeStarts[visibleGraphemes];
        var formatted = new FormattedMessage();
        formatted.AddMarkupPermissive($"[color={_textColor.ToHex()}]{FormattedMessage.EscapeText(_visibleText[..end])}[/color]");
        _label.SetMessage(formatted, AllowedTags);
    }

    private static int[] GetTextElementStarts(string text)
    {
        var starts = new List<int>();

        for (var index = 0; index < text.Length;)
        {
            starts.Add(index);
            var scalar = ReadScalar(text, index, out var scalarLength);
            index += scalarLength;

            if (IsRegionalIndicator(scalar) && index < text.Length)
            {
                var nextScalar = ReadScalar(text, index, out var nextScalarLength);
                if (IsRegionalIndicator(nextScalar))
                    index += nextScalarLength;
            }

            while (index < text.Length)
            {
                var nextScalar = ReadScalar(text, index, out var nextScalarLength);
                if (IsTextElementExtension(nextScalar))
                {
                    index += nextScalarLength;
                    continue;
                }

                if (nextScalar != 0x200D)
                    break;

                index += nextScalarLength;
                if (index >= text.Length)
                    break;

                ReadScalar(text, index, out nextScalarLength);
                index += nextScalarLength;
            }
        }

        return starts.ToArray();
    }

    private static int ReadScalar(string text, int index, out int length)
    {
        var current = text[index];
        if (current is >= '\uD800' and <= '\uDBFF'
            && index + 1 < text.Length
            && text[index + 1] is >= '\uDC00' and <= '\uDFFF')
        {
            length = 2;
            return 0x10000 + ((current - '\uD800') << 10) + (text[index + 1] - '\uDC00');
        }

        length = 1;
        return current;
    }

    private static bool IsTextElementExtension(int scalar)
    {
        return scalar is >= 0x0300 and <= 0x036F
            or >= 0x1AB0 and <= 0x1AFF
            or >= 0x1DC0 and <= 0x1DFF
            or >= 0x20D0 and <= 0x20FF
            or >= 0xFE00 and <= 0xFE0F
            or >= 0xFE20 and <= 0xFE2F
            or >= 0x1F3FB and <= 0x1F3FF
            or >= 0xE0100 and <= 0xE01EF;
    }

    private static bool IsRegionalIndicator(int scalar)
    {
        return scalar is >= 0x1F1E6 and <= 0x1F1FF;
    }
}
// Arcane-End
