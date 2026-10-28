using Content.Arcane.Shared.PDA;
using Content.Client.PDA;

namespace Content.Arcane.Client.PDA;

/// <summary>
/// Applies <see cref="PdaSkinPrototype"/> to PDA sprites and shows the screen state.
/// </summary>
public sealed partial class PdaSkinSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private const string FlashlightState = "light_overlay";
    private const string IdLightState = "id_overlay";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PdaScreenComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PdaScreenComponent, AppearanceChangeEvent>(OnAppearanceChange, after: [typeof(PdaVisualizerSystem)]);
    }

    private void OnStartup(Entity<PdaScreenComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite) || GetSkin(ent) is not { } skin)
            return;

        var spriteEnt = (ent.Owner, sprite);
        if (!_sprite.LayerMapTryGet(spriteEnt, PdaVisualizerSystem.PdaVisualLayers.Base, out var baseIndex, false))
            return;

        _sprite.LayerSetRsi(spriteEnt, baseIndex, skin.Sprite, skin.BaseState);
        _sprite.LayerSetColor(spriteEnt, baseIndex, skin.BaseColor ?? Color.White);

        var index = baseIndex + 1;
        foreach (var layer in skin.Layers)
        {
            _sprite.AddRsiLayer(spriteEnt, layer.State, skin.Sprite, index);
            _sprite.LayerSetColor(spriteEnt, index, layer.Color ?? Color.White);
            index++;
        }

        _sprite.AddRsiLayer(spriteEnt, ent.Comp.OffState, skin.Sprite, index);
        _sprite.LayerMapSet(spriteEnt, PdaScreenLayers.Screen, index);

        // Wide PDAs use other overlay states; the skin RSI only has the regular ones.
        if (_sprite.LayerMapTryGet(spriteEnt, PdaVisualizerSystem.PdaVisualLayers.Flashlight, out var flashlight, false))
            _sprite.LayerSetRsi(spriteEnt, flashlight, skin.Sprite, FlashlightState);
        if (_sprite.LayerMapTryGet(spriteEnt, PdaVisualizerSystem.PdaVisualLayers.IdLight, out var idLight, false))
            _sprite.LayerSetRsi(spriteEnt, idLight, skin.Sprite, IdLightState);

        if (TryComp<PdaBorderColorComponent>(ent, out var border))
        {
            if (skin.BorderColor is { } borderColor)
                border.BorderColor = borderColor.ToHex();
            if (skin.AccentColor is { } accentColor)
                border.AccentVColor = accentColor.ToHex();
        }

        if (TryComp<AppearanceComponent>(ent, out var appearance))
            UpdateScreen(ent, sprite, appearance);
    }

    private void OnAppearanceChange(Entity<PdaScreenComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite || GetSkin(ent) is not { } skin)
            return;

        // The PDA visualizer sets the base to the PDA type state, which the skin RSI doesn't have.
        _sprite.LayerSetRsi((ent, sprite), PdaVisualizerSystem.PdaVisualLayers.Base, skin.Sprite, skin.BaseState);
        UpdateScreen(ent, sprite, args.Component);
    }

    private void UpdateScreen(Entity<PdaScreenComponent> ent, SpriteComponent sprite, AppearanceComponent appearance)
    {
        if (!_appearance.TryGetData<string>(ent, PdaScreenVisuals.State, out var state, appearance)
            || !_sprite.LayerMapTryGet((ent, sprite), PdaScreenLayers.Screen, out var screen, false))
            return;

        _sprite.LayerSetRsiState((ent, sprite), screen, state);
    }

    /// <summary>
    /// The skin for this PDA's prototype or its closest parent.
    /// </summary>
    private PdaSkinPrototype? GetSkin(EntityUid uid)
    {
        if (MetaData(uid).EntityPrototype is not { } proto)
            return null;

        if (_proto.TryIndex<PdaSkinPrototype>(proto.ID, out var skin))
            return skin;

        foreach (var parent in _proto.EnumerateAllParents<EntityPrototype>(proto.ID))
        {
            if (_proto.TryIndex(parent.id, out skin))
                return skin;
        }

        return null;
    }
}
