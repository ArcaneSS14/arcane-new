using Content.Arcane.Shared.Economy;
using Robust.Client.GameObjects;

namespace Content.Arcane.Client.Economy;

public sealed partial class CreditHolochipVisualizerSystem : VisualizerSystem<CreditHolochipComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, CreditHolochipComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite)
            return;

        if (AppearanceSystem.TryGetData<string>(uid, CreditHolochipVisuals.BaseState, out var baseState, args.Component))
            SpriteSystem.LayerSetRsiState((uid, sprite), "base", baseState);

        if (AppearanceSystem.TryGetData<string>(uid, CreditHolochipVisuals.OverlayState, out var overlayState, args.Component))
            SpriteSystem.LayerSetRsiState((uid, sprite), "overlay", overlayState);

        if (AppearanceSystem.TryGetData<Color>(uid, CreditHolochipVisuals.BaseColor, out var color, args.Component))
            SpriteSystem.LayerSetColor((uid, sprite), "base", color);
    }
}
