using Content.Arcane.Common.SiliconStanding;
using Content.Arcane.Shared.SiliconStanding;
using Content.Shared.Mobs;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Toggleable;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Client.SiliconStanding;

public sealed partial class SiliconRestingVisualSystem : EntitySystem
{
    [Dependency] private AppearanceSystem _appearance = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SiliconStandingComponent, BorgRestVisualsChangedEvent>(OnRestChanged);
        SubscribeLocalEvent<SiliconStandingComponent, BorgVisualsUpdatedEvent>(OnBorgVisualsUpdated);
    }

    private void OnRestChanged(Entity<SiliconStandingComponent> ent, ref BorgRestVisualsChangedEvent args)
    {
        Refresh(ent.Owner, args.Resting);
    }

    private void OnBorgVisualsUpdated(Entity<SiliconStandingComponent> ent, ref BorgVisualsUpdatedEvent args)
    {
        Refresh(ent.Owner, HasComp<SiliconRestingComponent>(ent));
    }

    private void Refresh(EntityUid uid, bool resting)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite) ||
            !TryComp<BorgChassisComponent>(uid, out var borg) ||
            !TryComp<BorgSwitchableSubtypeComponent>(uid, out var subtype) ||
            !_prototypes.TryIndex(subtype.BorgSubtype, out var subtypeProto) ||
            !_prototypes.TryIndex(subtypeProto.ParentBorgType, out var typeProto))
            return;

        var spriteEnt = (uid, sprite);
        if (_sprite.LayerMapTryGet(spriteEnt, BorgVisualLayers.Body, out var body, false))
            _sprite.LayerSetRsiState(spriteEnt, body, resting && subtypeProto.Visuals.RestBodyState is { } state ? state : typeProto.SpriteBodyState);

        var alive = !_appearance.TryGetData<MobState>(uid, MobStateVisuals.State, out var mobState) || mobState == MobState.Alive;
        var hasPlayer = _appearance.TryGetData<bool>(uid, BorgVisuals.HasPlayer, out var player) && player;
        if (_sprite.LayerMapTryGet(spriteEnt, BorgVisualLayers.Light, out _, false))
            _sprite.LayerSetVisible(spriteEnt, BorgVisualLayers.Light, !resting && alive && (borg.BrainEntity != null || hasPlayer));

        if (_sprite.LayerMapTryGet(spriteEnt, BorgVisualLayers.LightStatus, out _, false))
        {
            var enabled = _appearance.TryGetData<bool>(uid, ToggleableVisuals.Enabled, out var light) && light;
            _sprite.LayerSetVisible(spriteEnt, BorgVisualLayers.LightStatus, !resting && enabled);
        }
    }
}
