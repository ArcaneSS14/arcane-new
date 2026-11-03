// Arcane-Start
using System.Globalization;
using System.Linq;
using Content.Client.Gameplay;
using Content.Client.Lobby;
using Content.Client.MainMenu;
using Content.Client.Options.UI;
using Content.Shared.Localizations;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.Log;

namespace Content.Client.Localizations;

/// <summary>
/// Applies the client's archived localization culture without requiring a restart.
/// </summary>
public sealed partial class DynamicLocalizationManager
{
    private const string FallbackCultureName = "en-US";

    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] private ContentLocalizationManager _contentLocalization = default!;
    [Dependency] private ILocalizationManager _localization = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private IStateManager _stateManager = default!;
    [Dependency] private IUserInterfaceManager _uiManager = default!;

    private ISawmill _sawmill = default!;
    private readonly List<Action<CultureInfo, CultureInfo>> _cultureChangedHandlers = [];
    private bool _changingCulture;
    private bool _uiReady;

    public event Action<CultureInfo, CultureInfo>? CultureChanged
    {
        add
        {
            if (value != null)
                _cultureChangedHandlers.Add(value);
        }
        remove
        {
            if (value == null)
                return;

            for (var index = _cultureChangedHandlers.Count - 1; index >= 0; index--)
            {
                if (!object.Equals(_cultureChangedHandlers[index], value))
                    continue;

                _cultureChangedHandlers.RemoveAt(index);
                return;
            }
        }
    }

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("dynamic-localization");
        _configuration.OnValueChanged(CVars.LocCultureName, OnCultureChanged, true);
        _uiReady = true;
    }

    public List<CultureInfo> GetAvailableCultures()
    {
        var current = _localization.DefaultCulture?.Name;
        return _localization.GetFoundCultures()
            .DistinctBy(culture => culture.Name, StringComparer.OrdinalIgnoreCase)
            .OrderBy(culture => culture.Name.Equals(current, StringComparison.OrdinalIgnoreCase) ? 0
                : culture.Name.Equals(FallbackCultureName, StringComparison.OrdinalIgnoreCase) ? 1
                : 2)
            .ThenBy(culture => culture.NativeName, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public static string GetCultureLabel(CultureInfo culture)
    {
        return $"{culture.NativeName} ({culture.Name})";
    }

    private void OnCultureChanged(string cultureName)
    {
        if (_changingCulture)
            return;

        var previous = _localization.DefaultCulture ?? CultureInfo.GetCultureInfo(FallbackCultureName);
        if (TryApplyCulture(cultureName, out var culture))
        {
            if (previous.Name.Equals(culture.Name, StringComparison.OrdinalIgnoreCase))
                return;

            if (_uiReady)
                RefreshUi();

            RaiseCultureChanged(previous, culture);
            return;
        }

        _sawmill.Warning("Rejected unavailable localization culture {Culture}; keeping {PreviousCulture}.",
            cultureName,
            previous.Name);

        _changingCulture = true;
        try
        {
            _configuration.SetCVar(CVars.LocCultureName, previous.Name);
            _configuration.SaveToFile();
        }
        finally
        {
            _changingCulture = false;
        }
    }

    private bool TryApplyCulture(string cultureName, out CultureInfo culture)
    {
        culture = default!;
        var available = GetAvailableCultures();
        var selected = available.FirstOrDefault(found =>
            found.Name.Equals(cultureName, StringComparison.OrdinalIgnoreCase));

        if (selected == null)
            return false;

        var previous = _localization.DefaultCulture;
        try
        {
            var fallback = CultureInfo.GetCultureInfo(FallbackCultureName);
            _contentLocalization.LoadCulture(fallback);
            _contentLocalization.LoadCulture(selected);

            SetFallbackCulture(selected, fallback);

            _localization.SetCulture(selected);
            // SetCulture does not invalidate entity localization caches on this engine version.
            _localization.ReloadLocalizations();
            culture = selected;
            return true;
        }
        catch (Exception exception)
        {
            _sawmill.Error("Failed to apply localization culture {Culture}: {Exception}",
                selected.Name,
                exception);

            if (previous != null)
            {
                _localization.SetCulture(previous);
                SetFallbackCulture(previous, CultureInfo.GetCultureInfo(FallbackCultureName));
            }

            return false;
        }
    }

    private void SetFallbackCulture(CultureInfo culture, CultureInfo fallback)
    {
        if (culture.Name.Equals(FallbackCultureName, StringComparison.OrdinalIgnoreCase))
            _localization.SetFallbackCulture();
        else
            _localization.SetFallbackCulture(fallback);
    }

    private void RaiseCultureChanged(CultureInfo previous, CultureInfo current)
    {
        var handlers = _cultureChangedHandlers.ToArray();
        foreach (var handler in handlers)
        {
            try
            {
                handler(previous, current);
            }
            catch (Exception exception)
            {
                _sawmill.Error("A culture change subscriber failed: {Exception}", exception);
            }
        }
    }

    private void RefreshUi()
    {
        switch (_stateManager.CurrentState)
        {
            case MainScreen mainScreen:
                mainScreen.ReloadLocalization();
                break;
            case LobbyState lobbyState:
                lobbyState.ReloadLocalization();
                break;
            case GameplayState gameplayState:
                gameplayState.ReloadLocalization();
                break;
        }

        if (_uiManager.TryGetFirstWindow<OptionsMenu>(out var optionsMenu))
            optionsMenu?.ReloadLocalization();
    }
}
// Arcane-End
