// Arcane-Start
using Content.Client.UserInterface.Motion;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Chat.UI;

public abstract partial class SpeechBubble
{
    private RichTextLabel? _typingLabel;
    private UiTextReveal? _reveal;
    private float? _originalOutline;
    private float _motionAge;
    private int _revealed = -1;

    protected void InitializeTyping(RichTextLabel label, FormattedMessage message)
    {
        label.SetMessage(message, AllowedTags);
        if (!ConfigManager.GetCVar(UiMotionSettings.Enabled) || !ConfigManager.GetCVar(UiMotionSettings.SpeechTyping))
            return;
        _typingLabel = label;
        _reveal = new UiTextReveal(message);
        _originalOutline = label.OutlineThicknessOverride;
        // Engine outlines ignore text alpha. Disable them during reveal so the hidden suffix stays hidden.
        label.OutlineThicknessOverride = 0;
        label.SetMessage(_reveal.Reveal(0), AllowedTags);
        _revealed = 0;
    }

    private void UpdateSpeechMotion(float delta, float timeLeft)
    {
        _motionAge += delta;
        if (_typingLabel == null || _reveal == null)
            return;
        var speed = UiMotionSettings.PlaybackSpeed(ConfigManager);
        var enabled = ConfigManager.GetCVar(UiMotionSettings.Enabled) && ConfigManager.GetCVar(UiMotionSettings.SpeechTyping);
        var pause = ConfigManager.GetCVar(UiMotionSettings.SpeechPunctuation)
            ? UiMotionSettings.Read(ConfigManager, UiMotionSettings.SpeechPunctuationMs, 0, 500) / 1000 / speed : 0;
        var count = !enabled || timeLeft <= BubbleFadeSeconds ? _reveal.Count : _reveal.VisibleCount(_motionAge,
            UiMotionSettings.Read(ConfigManager, UiMotionSettings.SpeechCharactersPerSecond, 10, 120) * speed,
            pause, UiMotionSettings.Read(ConfigManager, UiMotionSettings.SpeechMaxSeconds, 0.1f, 2.5f));
        count = Math.Max(_revealed, count);
        if (count != _revealed)
        {
            _typingLabel.SetMessage(_reveal.Reveal(count), AllowedTags);
            _revealed = count;
        }
        if (count != _reveal.Count)
            return;
        _typingLabel.OutlineThicknessOverride = _originalOutline;
        _typingLabel = null;
        _reveal = null;
    }

    private bool BubbleMovementEnabled => ConfigManager.GetCVar(UiMotionSettings.Enabled)
                                          && ConfigManager.GetCVar(UiMotionSettings.BubbleMovement)
                                          && !ConfigManager.GetCVar(UiMotionSettings.Reduce);

    private float BubbleSpeed => UiMotionSettings.PlaybackSpeed(ConfigManager);

    private float BubbleFadeSeconds => ConfigManager.GetCVar(UiMotionSettings.Enabled)
                                       && ConfigManager.GetCVar(UiMotionSettings.BubbleClosing)
        ? Math.Min(ConfigManager.GetCVar(UiMotionSettings.Reduce) ? 100 : 1000,
            UiMotionSettings.Read(ConfigManager, UiMotionSettings.BubbleExitMs, 0, 1000)) / 1000 / BubbleSpeed : 0;

    private float BubbleOpacity(float timeLeft)
    {
        if (!ConfigManager.GetCVar(UiMotionSettings.Enabled))
            return 1;
        var duration = UiMotionSettings.Read(ConfigManager, UiMotionSettings.BubbleEnterMs, 0, 1000) / 1000;
        if (ConfigManager.GetCVar(UiMotionSettings.Reduce))
            duration = Math.Min(duration, 0.1f);
        var speed = UiMotionSettings.PlaybackSpeed(ConfigManager);
        var openingProgress = !ConfigManager.GetCVar(UiMotionSettings.BubbleOpening) || duration <= 0
            ? 1 : Math.Clamp(_motionAge * speed / duration, 0, 1);
        var opening = UiMotionEasing.Effects(openingProgress, duration);
        var fadeDuration = BubbleFadeSeconds;
        var closingProgress = fadeDuration > 0 ? 1 - Math.Clamp(timeLeft / fadeDuration, 0, 1) : 0;
        var closing = 1 - UiMotionEasing.Effects(closingProgress, fadeDuration);
        return opening * closing;
    }
}
// Arcane-End
