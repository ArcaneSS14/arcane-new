using Content.Arcane.Shared.HeatTint;
using Content.Client.Chemistry.Visualizers;
using Robust.Client.GameObjects;
using Robust.Shared.Reflection;

namespace Content.Arcane.Client.HeatTint;

public sealed partial class HeatTintSystem : SharedHeatTintSystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private IReflectionManager _reflection = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HeatTintComponent, AppearanceChangeEvent>(OnAppearanceChanged,
            after: [typeof(SolutionContainerVisualsSystem)]);
        SubscribeLocalEvent<HeatTintComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnAppearanceChanged(Entity<HeatTintComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null ||
            !_appearance.TryGetData<float>(ent.Owner, HeatTintVisuals.Temperature, out var temperature, args.Component))
            return;

        var heatColor = GetHeatColor(ent.Comp.ColorGradient, temperature);
        if (ent.Comp.AffectedLayers is not { Count: > 0 } layers)
        {
            Apply(ent, args.Sprite, -1, args.Sprite.Color, heatColor);
            return;
        }

        foreach (var key in layers)
        {
            int index;
            var found = _reflection.TryParseEnumReference(key, out var parsed) && parsed != null
                ? _sprite.LayerMapTryGet((ent.Owner, args.Sprite), parsed, out index, false)
                : _sprite.LayerMapTryGet((ent.Owner, args.Sprite), key, out index, false);
            if (found &&
                _sprite.TryGetLayer((ent.Owner, args.Sprite), index, out var layer, false))
                Apply(ent, args.Sprite, index, layer.Color, heatColor);
        }
    }

    private void Apply(Entity<HeatTintComponent> ent, SpriteComponent sprite, int index, Color current, Color heatColor)
    {
        if (!ent.Comp.BaseColors.ContainsKey(index) ||
            ent.Comp.LastAppliedColors.TryGetValue(index, out var last) && current != last)
            ent.Comp.BaseColors[index] = current;

        var tinted = ent.Comp.BaseColors[index] * heatColor;
        if (index == -1)
            _sprite.SetColor((ent.Owner, sprite), tinted);
        else
            _sprite.LayerSetColor((ent.Owner, sprite), index, tinted);
        ent.Comp.LastAppliedColors[index] = tinted;
    }

    private void OnShutdown(Entity<HeatTintComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        foreach (var (index, original) in ent.Comp.BaseColors)
        {
            if (!ent.Comp.LastAppliedColors.TryGetValue(index, out var last))
                continue;
            if (index == -1)
            {
                if (sprite.Color == last)
                    _sprite.SetColor((ent.Owner, sprite), original);
            }
            else if (_sprite.TryGetLayer((ent.Owner, sprite), index, out var layer, false) && layer.Color == last)
                _sprite.LayerSetColor((ent.Owner, sprite), index, original);
        }

        ent.Comp.BaseColors.Clear();
        ent.Comp.LastAppliedColors.Clear();
    }
}
