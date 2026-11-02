// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Arcane.Common.CCVar;

[CVarDefs]
public sealed partial class ArcaneCVars
{
    /// <summary>
    /// Controls validation of saved tile variants when a grid initializes: off, log, on.
    /// </summary>
    public static readonly CVarDef<string> InvalidTileVariantMode =
        CVarDef.Create("tiles.invalid_variant_mode", "on", CVar.SERVERONLY);

    #region Sprinting

    /// <summary>
    ///     Is sprinting enabled
    /// </summary>
    public static readonly CVarDef<bool> SprintEnabled =
        CVarDef.Create("sprint.sprint_enabled", true, CVar.SERVER | CVar.REPLICATED);

    #endregion

    #region Client customization

    public static readonly CVarDef<bool> ChatRunechat =
        CVarDef.Create("chat.runechat", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> ChatRunechatFontSize =
        CVarDef.Create("chat.runechat_font_size", 12f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> ChatRunechatWhisperFontSize =
        CVarDef.Create("chat.runechat_whisper_font_size", 9f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> ChatRunechatFont =
        CVarDef.Create("chat.runechat_font", "grand9k", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> ChatRunechatWhisperFont =
        CVarDef.Create("chat.runechat_whisper_font", "grand9k", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> ChatRunechatOutline =
        CVarDef.Create("chat.runechat_outline", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> ChatRunechatRandomColor =
        CVarDef.Create("chat.runechat_random_color", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> ChatRunechatColor =
        CVarDef.Create("chat.runechat_color", "#FFFFFFFF", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> ChatRunechatTypingAnimation =
        CVarDef.Create("chat.runechat_typing_animation", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> ChatRunechatTypingSpeed =
        CVarDef.Create("chat.runechat_typing_speed", 30f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> ChatRunechatPostTypingLifetime =
        CVarDef.Create("chat.runechat_post_typing_lifetime", 2f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> UseCustomInteractionOutlineColors =
        CVarDef.Create("interaction_outline.use_custom_colors", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> ValidInteractionOutlineColor =
        CVarDef.Create("interaction_outline.in_range_color", "#00FF0055", CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> InvalidInteractionOutlineColor =
        CVarDef.Create("interaction_outline.out_of_range_color", "#FF000055", CVar.CLIENTONLY | CVar.ARCHIVE);

    #endregion

    #region Radio channel colors

    public static readonly CVarDef<bool> UseCustomRadioChannelColors =
        CVarDef.Create("radio.use_custom_channel_colors", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Semicolon-delimited <c>channelId=#RRGGBBAA</c> color overrides.
    /// </summary>
    public static readonly CVarDef<string> RadioChannelColors =
        CVarDef.Create("radio.channel_colors", string.Empty, CVar.CLIENTONLY | CVar.ARCHIVE);

    #endregion

}
