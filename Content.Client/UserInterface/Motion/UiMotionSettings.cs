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
    public static readonly CVarDef<float> MenuEnterMs = Float("menu_enter_ms", 350);
    public static readonly CVarDef<float> MenuExitMs = Float("menu_exit_ms", 350);
    public static readonly CVarDef<float> MenuTravel = Float("menu_travel", 6);
    public static readonly CVarDef<float> MenuSequenceMs = Float("menu_sequence_ms", 110);

    public static readonly CVarDef<bool> Windows = Bool("windows");
    public static readonly CVarDef<float> WindowEnterMs = Float("window_enter_ms", 350);
    public static readonly CVarDef<float> WindowTravel = Float("window_travel", 8);
    public static readonly CVarDef<bool> Buttons = Bool("buttons");
    public static readonly CVarDef<float> ButtonEnterMs = Float("button_enter_ms", 150);
    public static readonly CVarDef<float> ButtonIntervalMs = Float("button_interval_ms", 25);
    public static readonly CVarDef<float> ButtonJitterMs = Float("button_jitter_ms", 10);
    public static readonly CVarDef<float> ButtonMaxDelayMs = Float("button_max_delay_ms", 180);
    // 0: together, 1: ordered, 2: ordered with jitter, 3: random.
    public static readonly CVarDef<int> ButtonOrder = CVarDef.Create("ui.motion_button_order", 2, CVar.CLIENTONLY | CVar.ARCHIVE);
    public static readonly CVarDef<bool> Checkboxes = Bool("checkboxes");
    public static readonly CVarDef<float> CheckboxMs = Float("checkbox_ms", 150);
    public static readonly CVarDef<float> CheckboxIntensity = Float("checkbox_intensity", 0.35f);

    public static readonly CVarDef<bool> Reduce = CVarDef.Create("ui.motion_reduce", false, CVar.CLIENTONLY | CVar.ARCHIVE);
    public static readonly CVarDef<bool> Popups = Bool("popups");
    public static readonly CVarDef<float> PopupsMs = Float("popups_ms", 150);
    public static readonly CVarDef<bool> Slots = Bool("slots");
    public static readonly CVarDef<float> SlotEnterMs = Float("slot_enter_ms", 150);
    public static readonly CVarDef<float> SlotExitMs = Float("slot_exit_ms", 150);
    public static readonly CVarDef<float> SlotScale = Float("slot_scale", 0.08f);
    public static readonly CVarDef<bool> SlotHighlight = Bool("slot_highlight");
    public static readonly CVarDef<bool> Actions = Bool("actions");
    public static readonly CVarDef<float> ActionsEnterMs = Float("actions_enter_ms", 150);
    public static readonly CVarDef<bool> ActionsCooldownPulse = Bool("actions_cooldown_pulse");
    public static readonly CVarDef<bool> Alerts = Bool("alerts");
    public static readonly CVarDef<float> AlertsEnterMs = Float("alerts_enter_ms", 200);
    public static readonly CVarDef<float> AlertsExitMs = Float("alerts_exit_ms", 150);
    public static readonly CVarDef<bool> AlertsSeverityPulse = Bool("alerts_severity_pulse");
    public static readonly CVarDef<bool> Chat = Bool("chat");
    public static readonly CVarDef<float> ChatMs = Float("chat_ms", 150);
    public static readonly CVarDef<bool> Notifications = Bool("notifications");
    public static readonly CVarDef<float> NotificationsMs = Float("notifications_ms", 200);
    public static readonly CVarDef<bool> Progress = Bool("progress");
    public static readonly CVarDef<float> ProgressMs = Float("progress_ms", 200);
    public static readonly CVarDef<bool> Radial = Bool("radial");
    public static readonly CVarDef<float> RadialEnterMs = Float("radial_enter_ms", 350);
    public static readonly CVarDef<float> RadialExitMs = Float("radial_exit_ms", 150);
    public static readonly CVarDef<bool> RadialHover = Bool("radial_hover");
    public static readonly CVarDef<float> RadialHoverScale = Float("radial_hover_scale", 0.06f);
    public static readonly CVarDef<bool> Lists = Bool("lists");
    public static readonly CVarDef<float> ListsEnterMs = Float("lists_enter_ms", 200);
    public static readonly CVarDef<float> ListsMaxStagger = Float("lists_max_stagger", 8);
    public static readonly CVarDef<bool> Tabs = Bool("tabs");
    public static readonly CVarDef<float> TabsEnterMs = Float("tabs_enter_ms", 200);
    public static readonly CVarDef<bool> Votes = Bool("votes");
    public static readonly CVarDef<float> VotesEnterMs = Float("votes_enter_ms", 350);
    public static readonly CVarDef<float> VotesTravel = Float("votes_travel", 48);

    public static readonly CVarDef<bool> BubbleOpening = Bool("bubble_opening");
    public static readonly CVarDef<bool> BubbleClosing = Bool("bubble_closing");
    public static readonly CVarDef<bool> BubbleMovement = Bool("bubble_movement");
    public static readonly CVarDef<float> BubbleEnterMs = Float("bubble_enter_ms", 200);
    public static readonly CVarDef<float> BubbleExitMs = Float("bubble_exit_ms", 200);
    public static readonly CVarDef<bool> SpeechTyping = Bool("speech_typing");
    public static readonly CVarDef<float> SpeechCharactersPerSecond = Float("speech_characters_per_second", 45);
    public static readonly CVarDef<float> SpeechMaxSeconds = Float("speech_max_seconds", 1.5f);
    public static readonly CVarDef<bool> SpeechPunctuation = Bool("speech_punctuation");
    public static readonly CVarDef<float> SpeechPunctuationMs = Float("speech_punctuation_ms", 100);

    private static CVarDef<bool> Bool(string name) => CVarDef.Create("ui.motion_" + name, true, CVar.CLIENTONLY | CVar.ARCHIVE);
    private static CVarDef<float> Float(string name, float value) => CVarDef.Create("ui.motion_" + name, value, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static bool IsEnabled(IConfigurationManager configuration, CVarDef<bool> group) =>
        configuration.GetCVar(Enabled) && configuration.GetCVar(group);

    public static float PlaybackSpeed(IConfigurationManager configuration) =>
        configuration.GetCVar(Reduce) ? 1 : NormalizeSpeed(configuration.GetCVar(Speed));

    public static UiMotionProfile Resolve(IConfigurationManager configuration, UiMotionProfile profile)
    {
        if (!configuration.GetCVar(Reduce))
            return profile;

        var enterEffect = profile.EnterEffectDuration ?? profile.EnterDuration;
        var exitEffect = profile.ExitEffectDuration ?? profile.ExitDuration;
        if (profile is { Travel: 0, Overshoot: false, Sequence: false }
            && profile.EnterDuration.TotalMilliseconds <= 100 && profile.ExitDuration.TotalMilliseconds <= 100
            && enterEffect.TotalMilliseconds <= 100 && exitEffect.TotalMilliseconds <= 100)
            return profile;

        return profile with
        {
            EnterDuration = TimeSpan.FromMilliseconds(Math.Min(100, profile.EnterDuration.TotalMilliseconds)),
            ExitDuration = TimeSpan.FromMilliseconds(Math.Min(100, profile.ExitDuration.TotalMilliseconds)),
            EnterEffectDuration = TimeSpan.FromMilliseconds(Math.Min(100,
                enterEffect.TotalMilliseconds)),
            ExitEffectDuration = TimeSpan.FromMilliseconds(Math.Min(100,
                exitEffect.TotalMilliseconds)),
            Travel = 0,
            Overshoot = false,
            Sequence = false
        };
    }

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
        var enter = Read(configuration, MenuEnterMs, 0, 1000);
        var exit = Read(configuration, MenuExitMs, 0, 1000);
        return new UiMotionProfile(
            TimeSpan.FromMilliseconds(enter),
            TimeSpan.FromMilliseconds(exit),
            Read(configuration, MenuTravel, 0, 24), configuration.GetCVar(MenuSpring))
        {
            EnterEffectDuration = SpatialEffectDuration(enter),
            ExitEffectDuration = SpatialEffectDuration(exit),
            Fade = configuration.GetCVar(MenuFade),
            Opening = configuration.GetCVar(MenuOpening),
            Sequence = configuration.GetCVar(MenuSequence),
            SequenceSeconds = Read(configuration, MenuSequenceMs, 0, 500) / 1000
        };
    }

    public static TimeSpan SpatialEffectDuration(float spatialMilliseconds)
    {
        var milliseconds = spatialMilliseconds <= UiMotionEasing.FastSpatialSeconds * 1000
            ? UiMotionEasing.FastEffectsSeconds * 1000
            : spatialMilliseconds <= UiMotionEasing.DefaultSpatialSeconds * 1000
                ? UiMotionEasing.DefaultEffectsSeconds * 1000
                : UiMotionEasing.SlowEffectsSeconds * 1000;
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    public static float NormalizeSpeed(float value)
    {
        return float.IsFinite(value) ? Math.Clamp(value, MinSpeed, MaxSpeed) : Speed.DefaultValue;
    }
}
// Arcane-End
