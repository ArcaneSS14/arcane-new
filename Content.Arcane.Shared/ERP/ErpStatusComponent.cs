using Content.Arcane.Common.Preferences;
using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.ERP;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ErpStatusComponent : Component
{
    [DataField, AutoNetworkedField]
    public ErpPreference Preference = ErpPreference.Ask;
}
