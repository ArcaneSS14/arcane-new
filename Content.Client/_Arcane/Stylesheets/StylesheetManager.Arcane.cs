using Robust.Client.UserInterface;

namespace Content.Client.Stylesheets;

/// <summary>
/// Lets Arcane client code swap the default stylesheets for a colour theme.
/// </summary>
public sealed partial class StylesheetManager
{
    private Stylesheet? _stockNanotrasen;
    private Stylesheet? _stockSystem;

    /// <summary>
    /// Uses <paramref name="stylesheet"/> for both the Nanotrasen and System styles, or the stock ones when null,
    /// and re-styles the open UI.
    /// </summary>
    public void ApplyThemeStylesheet(Stylesheet? stylesheet)
    {
        _stockNanotrasen ??= SheetNanotrasen;
        _stockSystem ??= SheetSystem;

        var nanotrasen = stylesheet ?? _stockNanotrasen;
        var system = stylesheet ?? _stockSystem;
        if (ReferenceEquals(nanotrasen, SheetNanotrasen) && ReferenceEquals(system, SheetSystem))
            return;

        var previousNanotrasen = SheetNanotrasen;
        var previousSystem = SheetSystem;
        SheetNanotrasen = nanotrasen;
        SheetSystem = system;
        _userInterfaceManager.Stylesheet = nanotrasen;
        UpdateExplicitStylesheets(_userInterfaceManager.RootControl, previousNanotrasen, previousSystem, nanotrasen, system);
    }

    private static void UpdateExplicitStylesheets(
        Control control,
        Stylesheet previousNanotrasen,
        Stylesheet previousSystem,
        Stylesheet nanotrasen,
        Stylesheet system)
    {
        if (ReferenceEquals(control.Stylesheet, previousNanotrasen))
            control.Stylesheet = nanotrasen;
        else if (ReferenceEquals(control.Stylesheet, previousSystem))
            control.Stylesheet = system;

        foreach (var child in control.Children)
            UpdateExplicitStylesheets(child, previousNanotrasen, previousSystem, nanotrasen, system);
    }
}
