using Content.Client.Stylesheets.Palette;
using Content.Arcane.Shared.DiscordRoles;

namespace Content.Arcane.Client.StyleSheets;

public sealed record ArcaneTheme(
    string Id,
    string NameLocId,
    DiscordRole? RequiredRole,
    ColorPalette Primary,
    ColorPalette Secondary,
    ColorPalette Buttons,
    ColorPalette Highlight,
    Color Accent
);
