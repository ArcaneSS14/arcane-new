using System.Linq;
using Content.Arcane.Shared.Cards;
using Robust.Client.GameObjects;

namespace Content.Arcane.Client.Cards;

public sealed partial class PlayingCardsVisualSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprites = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlayingCardsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<PlayingCardsComponent, AfterAutoHandleStateEvent>(OnState);
    }

    private void OnStartup(Entity<PlayingCardsComponent> ent, ref ComponentStartup args) => UpdateSprite(ent);
    private void OnState(Entity<PlayingCardsComponent> ent, ref AfterAutoHandleStateEvent args) => UpdateSprite(ent);

    private void UpdateSprite(Entity<PlayingCardsComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;
        var count = Math.Max(1, ent.Comp.VisibleStates.Count);
        var spriteEntity = (ent.Owner, sprite);
        while (sprite.AllLayers.Count() < count)
            _sprites.AddBlankLayer(spriteEntity);
        while (sprite.AllLayers.Count() > count)
            _sprites.RemoveLayer(spriteEntity, sprite.AllLayers.Count() - 1);
        for (var i = 0; i < count; i++)
        {
            var state = ent.Comp.VisibleStates.Count > 0 ? ent.Comp.VisibleStates[i] : "back_black";
            _sprites.LayerSetSprite(spriteEntity, i, new SpriteSpecifier.Rsi(new ResPath(PlayingCardsComponent.SpritePath), state));
            var rotation = Angle.Zero;
            var offset = Vector2.Zero;
            if (ent.Comp.Kind == PlayingCardKind.Hand)
            {
                var x = count == 1 ? 0 : -0.25f + i * 0.5f / (count - 1);
                offset = new Vector2(x, -(x * x) + 0.10f);
                rotation = Angle.FromDegrees(count == 1 ? 0 : 60 - i * 120f / (count - 1));
            }
            else if (ent.Comp.Kind == PlayingCardKind.Deck)
            {
                offset = new Vector2(0, 0.02f * i);
                rotation = Angle.FromDegrees(90);
            }
            _sprites.LayerSetOffset(spriteEntity, i, offset);
            _sprites.LayerSetRotation(spriteEntity, i, rotation);
        }
    }
}
