// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Arcane.Server.Medical.CrewMonitoring;

/// <summary>
/// Makes a powered crew monitoring console beep and glow red while a crew member with coordinate sensors
/// lies dead outside a morgue, anti-rot container or stasis bed (Orion #65, #274, #369).
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CrewMonitoringAlertComponent : Component
{
    /// <summary>
    /// Seconds between checks.
    /// </summary>
    [DataField]
    public TimeSpan AlertInterval = TimeSpan.FromSeconds(30);

    [DataField]
    public SoundSpecifier AlertSound = new SoundPathSpecifier("/Audio/Machines/twobeep.ogg",
        AudioParams.Default.WithVolume(-8f).WithVariation(0.25f));

    [DataField]
    public Color AlertLightColor = Color.Red;

    [DataField]
    public float AlertLightEnergy = 40f;

    [DataField]
    public float AlertLightRadius = 1.5f;

    [DataField, AutoPausedField]
    public TimeSpan NextAlert;

    /// <summary>
    /// Light settings to restore once the alert ends; null while not alerting.
    /// </summary>
    [ViewVariables]
    public (Color Color, float Energy, float Radius)? NormalLight;
}
