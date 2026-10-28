// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.ToggleableHUD;

/// <summary>
/// Gives the entity an action that toggles every station HUD overlay at once (Orion #246).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ToggleableHUDComponent : Component
{
    [DataField]
    public EntProtoId Action = "ActionToggleHUDs";

    [DataField]
    public EntityUid? ActionEntity;

    [DataField, AutoNetworkedField]
    public bool IsToggled;

    [DataField]
    public List<ProtoId<DamageContainerPrototype>> DamageContainers = new()
    {
        "Biological",
        "Inorganic",
        "Silicon",
        "SiliconRadiation",
    };

    [DataField]
    public LocId PopupToggleOn = "ghost-gui-toggle-hud-popup-on";

    [DataField]
    public LocId PopupToggleOff = "ghost-gui-toggle-hud-popup-off";
}
