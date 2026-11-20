// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Goobstation.Shared.StationRadio.Components;

/// <summary>Authoritative playback volume before the listener's station-radio volume setting.</summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class StationRadioPlaybackComponent : Component
{
    [AutoNetworkedField]
    public float BaseVolume = float.NegativeInfinity;
}
