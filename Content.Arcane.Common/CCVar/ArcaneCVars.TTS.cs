using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

public sealed partial class ArcaneCVars
{
    /// <summary>
    /// Whether TTS is enabled on the server.
    /// </summary>
    public static readonly CVarDef<bool> TTSEnabled =
        CVarDef.Create("tts.enabled", false, CVar.SERVER | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// URL of the TTS server API
    /// </summary>
    public static readonly CVarDef<string> TTSApiUrl =
        CVarDef.Create("tts.api_url", "", CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Auth token for the TTS server API
    /// </summary>
    public static readonly CVarDef<string> TTSApiToken =
        CVarDef.Create("tts.api_token", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    /// Amount of seconds before timeout for API.
    /// </summary>
    public static readonly CVarDef<int> TTSApiTimeout =
        CVarDef.Create("tts.api_timeout", 5, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    /// Count of in-memory cached tts voice lines.
    /// </summary>
    public static readonly CVarDef<int> TTSMaxCache =
        CVarDef.Create("tts.max_cache", 250, CVar.SERVERONLY | CVar.ARCHIVE);


    /// <summary>
    /// Tts rate limit values are accounted in periods of this size (seconds).
    /// After the period has passed, the count resets.
    /// </summary>
    public static readonly CVarDef<float> TTSRateLimitPeriod =
        CVarDef.Create("tts.rate_limit_period", 2f, CVar.SERVERONLY);

    /// <summary>
    /// How many tts preview messages are allowed in a single rate limit period.
    /// </summary>
    public static readonly CVarDef<int> TTSRateLimitCount =
        CVarDef.Create("tts.rate_limit_count", 3, CVar.SERVERONLY);


    /// <summary>
    /// TTS request timeout in seconds.
    /// </summary>
    public static readonly CVarDef<float> TTSRequestTimeout =
        CVarDef.Create("tts.timeout", 5f, CVar.SERVERONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Should the client use TTS instead of barks.
    /// </summary>
    public static readonly CVarDef<bool> UseTTS =
        CVarDef.Create("tts.use_tts", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Default volume setting of TTS sound
    /// </summary>
    public static readonly CVarDef<float> TTSVolume =
        CVarDef.Create("tts.volume", 0.5f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     TTS radio volume.
    /// </summary>
    public static readonly CVarDef<float> TTSRadioVolume =
        CVarDef.Create("tts.radio_volume", 0.5f, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Frequencies of radio channels on which TTS radio is disabled, separated by ';'.
    /// </summary>
    public static readonly CVarDef<string> TTSRadioChannelMuted =
        CVarDef.Create("tts.radio_channel_muted", "", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Volumes of TTS on radio channels: "frequency=multiplier;...", multiplier relative to <see cref="TTSRadioVolume"/>.
    /// </summary>
    public static readonly CVarDef<string> TTSRadioChannelVolumes =
        CVarDef.Create("tts.radio_channel_volumes", "", CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    ///     Whether radio transmissions are read out loud with TTS while the local player is a ghost.
    ///     Defaults to enabled; reset back to true whenever the player stops being a ghost.
    /// </summary>
    public static readonly CVarDef<bool> TTSGhostRadioUseTTS =
        CVarDef.Create("tts.ghost_radio_use_tts", true, CVar.CLIENTONLY);
}
