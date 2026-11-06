namespace Content.Client.Options.UI;

public sealed partial class OptionsMenu
{
    /// <summary>
    /// Allows client modules to attach their own options tabs.
    /// </summary>
    public static event Action<OptionsMenu>? ConfigureExtensions;

    /// <summary>
    /// Reloads values in attached options tabs alongside the standard tabs.
    /// </summary>
    public event Action? ReloadExtensions;
}
