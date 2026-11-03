// Arcane-Start
using Robust.Shared.Configuration;

namespace Content.Client.UserInterface.Motion;

/// <summary>Client-wide preferences shared by all opt-in UI transitions.</summary>
[CVarDefs]
public static class UiMotionSettings
{
    public const float MinSpeed = 0.25f;
    public const float MaxSpeed = 3f;

    public static readonly CVarDef<bool> Enabled =
        CVarDef.Create("ui.animations", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> AnimateClosing =
        CVarDef.Create("ui.animation_closing", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>Playback multiplier: larger values produce shorter transitions.</summary>
    public static readonly CVarDef<float> Speed =
        CVarDef.Create("ui.animation_speed", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> MenuOpening = Bool("menu_opening");
    public static readonly CVarDef<bool> MenuSequence = Bool("menu_sequence");
    public static readonly CVarDef<bool> MenuFade = Bool("menu_fade");
    public static readonly CVarDef<bool> MenuSpring = Bool("menu_spring");
    public static readonly CVarDef<float> MenuEnterMs = Float("menu_enter_ms", 250);
    public static readonly CVarDef<float> MenuExitMs = Float("menu_exit_ms", 100);
    public static readonly CVarDef<float> MenuTravel = Float("menu_travel", 6);
    public static readonly CVarDef<float> MenuSequenceMs = Float("menu_sequence_ms", 110);

    public static readonly CVarDef<bool> Windows = Bool("windows");
    public static readonly CVarDef<float> WindowEnterMs = Float("window_enter_ms", 220);
    public static readonly CVarDef<bool> Buttons = Bool("buttons");
    public static readonly CVarDef<float> ButtonEnterMs = Float("button_enter_ms", 150);
    public static readonly CVarDef<float> ButtonIntervalMs = Float("button_interval_ms", 25);
    public static readonly CVarDef<float> ButtonJitterMs = Float("button_jitter_ms", 10);
    public static readonly CVarDef<float> ButtonMaxDelayMs = Float("button_max_delay_ms", 180);
    // 0: together, 1: ordered, 2: ordered with jitter, 3: random.
    public static readonly CVarDef<int> ButtonOrder = CVarDef.Create("ui.motion_button_order", 2, CVar.CLIENTONLY | CVar.ARCHIVE);
    public static readonly CVarDef<bool> Checkboxes = Bool("checkboxes");
    public static readonly CVarDef<float> CheckboxMs = Float("checkbox_ms", 120);
    public static readonly CVarDef<float> CheckboxIntensity = Float("checkbox_intensity", 0.35f);

    public static readonly CVarDef<bool> BubbleOpening = Bool("bubble_opening");
    public static readonly CVarDef<bool> BubbleClosing = Bool("bubble_closing");
    public static readonly CVarDef<bool> BubbleMovement = Bool("bubble_movement");
    public static readonly CVarDef<float> BubbleEnterMs = Float("bubble_enter_ms", 180);
    public static readonly CVarDef<float> BubbleExitMs = Float("bubble_exit_ms", 250);
    public static readonly CVarDef<bool> SpeechTyping = Bool("speech_typing");
    public static readonly CVarDef<float> SpeechCharactersPerSecond = Float("speech_characters_per_second", 45);
    public static readonly CVarDef<float> SpeechMaxSeconds = Float("speech_max_seconds", 1.5f);
    public static readonly CVarDef<bool> SpeechPunctuation = Bool("speech_punctuation");
    public static readonly CVarDef<float> SpeechPunctuationMs = Float("speech_punctuation_ms", 100);

    private static CVarDef<bool> Bool(string name) => CVarDef.Create("ui.motion_" + name, true, CVar.CLIENTONLY | CVar.ARCHIVE);
    private static CVarDef<float> Float(string name, float value) => CVarDef.Create("ui.motion_" + name, value, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static float Read(IConfigurationManager configuration, CVarDef<float> setting, float min, float max)
    {
        return Normalize(configuration.GetCVar(setting), min, max, setting.DefaultValue);
    }

    public static float Normalize(float value, float min, float max, float fallback)
    {
        return Math.Clamp(float.IsFinite(value) ? value : fallback, min, max);
    }

    public static UiMotionProfile MenuProfile(IConfigurationManager configuration)
    {
        return new UiMotionProfile(
            TimeSpan.FromMilliseconds(Read(configuration, MenuEnterMs, 0, 1000)),
            TimeSpan.FromMilliseconds(Read(configuration, MenuExitMs, 0, 1000)),
            Read(configuration, MenuTravel, 0, 24), configuration.GetCVar(MenuSpring))
        {
            Fade = configuration.GetCVar(MenuFade),
            Opening = configuration.GetCVar(MenuOpening),
            Sequence = configuration.GetCVar(MenuSequence),
            SequenceSeconds = Read(configuration, MenuSequenceMs, 0, 500) / 1000
        };
    }

    public static float NormalizeSpeed(float value)
    {
        return float.IsFinite(value) ? Math.Clamp(value, MinSpeed, MaxSpeed) : Speed.DefaultValue;
    }
}
// Arcane-End
