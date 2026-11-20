using Content.Arcane.Client.DiscordRoles;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.DiscordRoles;
using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Stylesheets;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;

namespace Content.Arcane.Client.StyleSheets;

/// <summary>
/// Applies the UI colour theme chosen in the options. Sponsor-only themes fall back to the stock look until the
/// player's Discord roles allow them.
/// </summary>
[UsedImplicitly]
public sealed partial class ArcaneThemeUIController : UIController
{
    public const string StockThemeId = "nanotrasen";

    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IStylesheetManager _stylesheets = default!;
    [Dependency] private IStateManager _state = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private DiscordRoleManager _roles = default!;

    private readonly Dictionary<string, Stylesheet> _built = new();

    public override void Initialize()
    {
        base.Initialize();
        _cfg.OnValueChanged(ArcaneCVars.UiTheme, _ => Refresh());
        _roles.RolesUpdated += Refresh;
        // Stylesheets are ready once the first state is entered.
        _state.OnStateChanged += _ => Refresh();
    }

    public bool CanUse(ArcaneTheme theme)
    {
        if (theme.RequiredRole is not { } role)
            return true;

        return _player.LocalSession is { } session
               && (_roles.HasRole(session, role) || _roles.HasRole(session, DiscordRole.AdminBenefit));
    }

    private void Refresh()
    {
        if (_stylesheets is not StylesheetManager manager || manager.SheetNanotrasen == null)
            return;

        var requested = _cfg.GetCVar(ArcaneCVars.UiTheme);
        ArcaneTheme? selected = null;
        foreach (var theme in ArcanePalette.Themes)
        {
            if (theme.Id == requested && CanUse(theme))
                selected = theme;
        }

        if (selected == null)
        {
            manager.ApplyThemeStylesheet(null);
            return;
        }

        if (!_built.TryGetValue(selected.Id, out var stylesheet))
        {
            var sheet = new ArcaneStylesheet(new BaseStylesheet.NoConfig(), manager, $"Arcane{selected.Id}", selected);
            stylesheet = sheet.Stylesheet;
            _built[selected.Id] = stylesheet;
        }

        manager.ApplyThemeStylesheet(stylesheet);
    }
}
