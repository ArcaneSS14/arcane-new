# UltraVision

Adapted from DeltaV content in Goob-Station/Goob-Station at
`45e8d6b1cb1d3464762c8a50ce2f7f5ddf6e59c1`:

- `Content.Client/_DV/Overlay/UltraVisionSystem.cs`
- `Content.Client/_DV/Overlay/UltraVisionOverlay.cs`
- `Content.Shared/_DV/Abilities/UltraVisionComponent.cs`
- `Resources/Prototypes/_DV/Traits/altvision.yml`
- `Resources/Prototypes/_DV/Shaders/birdvision.yml`
- `Resources/Textures/_DV/Shaders/ultravision.swsl`

The upstream history credits Aidenkrz and deltanedas for the imported content and
namespace adaptation. Code and shader remain AGPL-3.0-or-later, as in the source
repository. The port uses Trauma's existing accessibility setting and restricts
rendering to the player's current eye. It adds no entities to the rendered view.
