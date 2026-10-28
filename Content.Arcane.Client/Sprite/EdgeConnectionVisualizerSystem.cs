using Content.Arcane.Shared.Sprite;
using Robust.Client.GameObjects;

namespace Content.Arcane.Client.Sprite;

/// <summary>
/// Swaps the configured sprite layer to its connected variant based on <see cref="EdgeConnectionVisuals.ConnectionMask"/>.
/// Only layers in one of <see cref="EdgeConnectionComponent.ConnectedStates"/> are changed.
/// </summary>
public sealed partial class EdgeConnectionVisualizerSystem : VisualizerSystem<EdgeConnectionComponent>
{
    /// <summary>
    /// Original layer state and RSI, remembered before the first swap.
    /// </summary>
    private readonly Dictionary<EntityUid, (string State, ResPath? Rsi)> _original = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EdgeConnectionComponent, ComponentShutdown>((uid, _, _) => _original.Remove(uid));
    }

    protected override void OnAppearanceChange(EntityUid uid, EdgeConnectionComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite
            || component.ConnectedSprite is not { } connectedRsi
            || !SpriteSystem.LayerMapTryGet((uid, sprite), component.Layer, out var layer, false))
            return;

        if (!_original.TryGetValue(uid, out var original))
        {
            if (!SpriteSystem.TryGetLayer((uid, sprite), layer, out var spriteLayer, false)
                || spriteLayer.State.Name is not { } stateName
                || !component.ConnectedStates.ContainsKey(stateName))
                return;

            original = (stateName, spriteLayer.ActualRsi?.Path);
            _original[uid] = original;
        }

        if (!component.ConnectedStates.TryGetValue(original.State, out var template))
            return;

        AppearanceSystem.TryGetData<EdgeConnectionDirections>(uid, EdgeConnectionVisuals.ConnectionMask, out var mask, args.Component);

        var east = (mask & EdgeConnectionDirections.East) != 0;
        var west = (mask & EdgeConnectionDirections.West) != 0;
        var variant = (east, west) switch
        {
            (true, true) => "l_r",
            (true, false) => "l",
            (false, true) => "r",
            _ => null,
        };

        if (variant == null)
        {
            if (original.Rsi is { } rsi)
                SpriteSystem.LayerSetRsi((uid, sprite), layer, rsi, original.State);
            else
                SpriteSystem.LayerSetRsiState((uid, sprite), layer, original.State);
            return;
        }

        SpriteSystem.LayerSetRsi((uid, sprite), layer, connectedRsi, string.Format(template, variant));
    }
}
