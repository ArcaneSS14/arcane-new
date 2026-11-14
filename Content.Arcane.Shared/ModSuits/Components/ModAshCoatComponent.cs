// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ModAshCoatComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Layers;

    [DataField, AutoNetworkedField]
    public int MaxLayers = 10;

    [DataField]
    public List<string> AccretionTilePrefixes = new();

    [DataField]
    public List<string> KeepTilePrefixes = new();

    [DataField]
    public List<EntProtoId> AccretionWeather = new();

    [DataField]
    public TimeSpan WeatherInterval = TimeSpan.FromSeconds(2);

    [DataField]
    public Dictionary<string, float> LayerReductions = new();

    [DataField]
    public float LayerExplosionCoefficient = 0.03f;

    [DataField]
    public float FullSpeedModifier = 1.1f;

    [DataField]
    public SoundSpecifier FullSound = new SoundPathSpecifier("/Audio/Effects/sparks1.ogg");

    [ViewVariables]
    public Vector2i? LastTile;

    [ViewVariables]
    public EntityUid? LastGrid;

    [ViewVariables]
    public TimeSpan NextWeatherLayer;
}
