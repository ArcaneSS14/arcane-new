using Content.Arcane.Shared.ModSuits.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Explosion;
using Content.Shared.Movement.Systems;
using Content.Shared.Weather;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Arcane.Shared.ModSuits.Systems;

public sealed partial class ModSuitSystem
{
    [Dependency] private SharedWeatherSystem _weather = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private ITileDefinitionManager _tileDefs = default!;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery = default!;

    private void InitializeAshCoat()
    {
        SubscribeLocalEvent<ModAshCoatComponent, DamageModifyEvent>(OnAshDamageModify);
        SubscribeLocalEvent<ModAshCoatComponent, GetExplosionResistanceEvent>(OnAshExplosionResistance);
        SubscribeLocalEvent<ModAshCoatComponent, RefreshMovementSpeedModifiersEvent>(OnAshRefreshSpeed);
        SubscribeLocalEvent<ModAshCoatComponent, ComponentShutdown>(OnAshShutdown);
    }

    private void OnAshDamageModify(EntityUid uid, ModAshCoatComponent coat, DamageModifyEvent args)
    {
        if (coat.Layers <= 0 || coat.LayerReductions.Count == 0)
            return;

        var modifiers = new DamageModifierSet();
        foreach (var (type, reduction) in coat.LayerReductions)
        {
            modifiers.Coefficients[type] = MathF.Max(0f, 1f - reduction * coat.Layers);
        }

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifiers);
    }

    private void OnAshExplosionResistance(Entity<ModAshCoatComponent> ent, ref GetExplosionResistanceEvent args)
    {
        args.DamageCoefficient *= MathF.Max(0f, 1f - ent.Comp.LayerExplosionCoefficient * ent.Comp.Layers);
    }

    private void OnAshRefreshSpeed(EntityUid uid, ModAshCoatComponent coat, RefreshMovementSpeedModifiersEvent args)
    {
        if (coat.LifeStage <= ComponentLifeStage.Running && coat.Layers >= coat.MaxLayers)
            args.ModifySpeed(coat.FullSpeedModifier, coat.FullSpeedModifier);
    }

    private void OnAshShutdown(Entity<ModAshCoatComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent) && ent.Comp.Layers >= ent.Comp.MaxLayers)
            _movement.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void UpdateAsh()
    {
        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<ModAshCoatComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var coat, out var xform))
        {
            if (coat.MaxLayers <= 0)
                continue;

            if (xform.GridUid is not { } gridUid
                || xform.ParentUid != gridUid
                || !_gridQuery.TryComp(gridUid, out var grid))
                continue;

            var tile = _map.GetTileRef(gridUid, grid, xform.Coordinates);
            var change = 0;

            if (coat.LastGrid != gridUid || coat.LastTile != tile.GridIndices)
            {
                var first = coat.LastTile == null;
                coat.LastGrid = gridUid;
                coat.LastTile = tile.GridIndices;
                if (!first && !tile.Tile.IsEmpty)
                    change = ClassifyTile(coat, _tileDefs[tile.Tile.TypeId].ID);
            }

            if (change <= 0 && now >= coat.NextWeatherLayer)
            {
                coat.NextWeatherLayer = now + coat.WeatherInterval;
                if (InAccretionWeather(coat, xform.MapID, gridUid, grid, tile))
                    change = 1;
            }

            if (change != 0)
                SetLayers((uid, coat), coat.Layers + change);
        }
    }

    private int ClassifyTile(ModAshCoatComponent coat, string tileId)
    {
        foreach (var prefix in coat.KeepTilePrefixes)
        {
            if (tileId.StartsWith(prefix, StringComparison.Ordinal))
                return 0;
        }

        foreach (var prefix in coat.AccretionTilePrefixes)
        {
            if (tileId.StartsWith(prefix, StringComparison.Ordinal))
                return 1;
        }

        return -1;
    }

    private bool InAccretionWeather(ModAshCoatComponent coat, MapId map, EntityUid gridUid, MapGridComponent grid, TileRef tile)
    {
        foreach (var id in coat.AccretionWeather)
        {
            if (_weather.HasWeather(map, id))
                return _weather.CanWeatherAffect((gridUid, grid, null), tile);
        }

        return false;
    }

    private void SetLayers(Entity<ModAshCoatComponent> ent, int layers)
    {
        layers = Math.Clamp(layers, 0, ent.Comp.MaxLayers);
        var old = ent.Comp.Layers;
        if (old == layers)
            return;

        ent.Comp.Layers = layers;
        Dirty(ent);

        var max = ent.Comp.MaxLayers;
        if (layers == max || old == max)
            _movement.RefreshMovementSpeedModifiers(ent.Owner);

        if (layers == max)
        {
            _popup.PopupEntity(Loc.GetString("modsuit-ash-full"), ent, ent);
            _audio.PlayPvs(ent.Comp.FullSound, ent);
        }
        else if (layers == 0)
        {
            _popup.PopupEntity(Loc.GetString("modsuit-ash-empty"), ent, ent);
        }
    }
}
