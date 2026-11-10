using Content.Arcane.Shared.MouseSwarm;

namespace Content.Arcane.Client.MouseSwarm;

public sealed partial class MouseSwarmSystem : EntitySystem
{
    [Dependency] private SpriteSystem _sprite = default!;

    private const string LayerPrefix = "mouse-swarm-";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MouseSwarmComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MouseSwarmComponent, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<MouseSwarmComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<MouseSwarmComponent> ent, ref ComponentShutdown args)
    {
        if (TryComp<SpriteComponent>(ent, out var sprite))
            ClearLayers((ent.Owner, sprite));
    }

    private void ClearLayers(Entity<SpriteComponent> ent)
    {
        for (var i = 0; _sprite.RemoveLayer(ent.AsNullable(), LayerPrefix + i, false); i++) { }
    }

    private void OnStartup(Entity<MouseSwarmComponent> ent, ref ComponentStartup args)
    {
        UpdateLayers(ent);
    }

    private void OnHandleState(Entity<MouseSwarmComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        UpdateLayers(ent);
    }

    private void UpdateLayers(Entity<MouseSwarmComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var spriteEnt = (ent.Owner, sprite);
        ClearLayers(spriteEnt);

        for (var i = 0; i < ent.Comp.Members.Count; i++)
        {
            var member = ent.Comp.Members[i];
            var index = _sprite.AddLayer(spriteEnt, new SpriteSpecifier.Rsi(member.Sprite, member.State));
            _sprite.LayerMapSet(spriteEnt, LayerPrefix + i, index);
            _sprite.LayerSetOffset(spriteEnt, index, member.Offset);
        }
    }
}
