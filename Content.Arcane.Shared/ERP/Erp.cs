using Content.Arcane.Common.Preferences;

namespace Content.Arcane.Shared.ERP;

public sealed class ErpPreferenceChangedEvent(ErpPreference oldPreference, ErpPreference newPreference) : EntityEventArgs
{
    public ErpPreference OldPreference = oldPreference;
    public ErpPreference NewPreference = newPreference;
}
