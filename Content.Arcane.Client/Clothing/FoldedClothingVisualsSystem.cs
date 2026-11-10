using Content.Arcane.Shared.Clothing;
using Content.Client.Clothing;
using Content.Shared.Clothing;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Foldable;
using Content.Shared.Item;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

namespace Content.Arcane.Client.Clothing;

/// <summary>
/// Substitutes only the normal uniform layer after the clothing owner has prepared it.
/// Folding authority and replicated state remain in FoldableSystem.
/// </summary>
public sealed partial class FoldedClothingVisualsSystem : EntitySystem
{
    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private SharedItemSystem _item = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FoldedClothingVisualsComponent, GetEquipmentVisualsEvent>(OnGetVisuals,
            after: [typeof(ClientClothingSystem)]);
        SubscribeLocalEvent<FoldedClothingVisualsComponent, FoldedEvent>(OnFolded,
            after: [typeof(FoldableClothingSystem)]);
        SubscribeLocalEvent<FoldedClothingVisualsComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FoldedClothingVisualsComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<FoldableComponent, ComponentShutdown>(OnFoldableShutdown);
    }

    private void OnGetVisuals(Entity<FoldedClothingVisualsComponent> ent, ref GetEquipmentVisualsEvent args)
    {
        if (!ent.Comp.Enabled ||
            args.Slot != ClientClothingSystem.Jumpsuit ||
            ent.Comp.LifeStage >= ComponentLifeStage.Stopping ||
            ent.Comp.RsiPath == null ||
            !TryComp<FoldableComponent>(ent, out var foldable) ||
            foldable.LifeStage >= ComponentLifeStage.Stopping ||
            !foldable.IsFolded ||
            !_cache.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / ent.Comp.RsiPath, out var resource))
            return;

        foreach (var (key, layer) in args.Layers)
        {
            // The selected prototypes use the default uniform layer. Do not replace
            // additional clothing effects or a different runtime skin's custom state.
            if (key != "jumpsuit-0" ||
                layer.State == null ||
                (layer.State != "equipped-INNERCLOTHING" &&
                 !layer.State.StartsWith("equipped-INNERCLOTHING-", StringComparison.Ordinal)))
                continue;

            var state = $"folded-{layer.State}";
            if (!resource.RSI.TryGetState(state, out _))
                continue;

            // ClothingSystem provided a per-request copy. Keep its key, scale,
            // color and other data, and let its species/displacement pass finish.
            layer.RsiPath = resource.RSI.Path.ToString();
            layer.State = state;
            break;
        }
    }

    private void OnFolded(Entity<FoldedClothingVisualsComponent> ent, ref FoldedEvent args)
    {
        // EquippedPrefix remains null because the folded RSI is separate.
        // A state change must still rebuild a uniform already worn by somebody.
        _item.VisualsChanged(ent);
    }

    private void OnStartup(Entity<FoldedClothingVisualsComponent> ent, ref ComponentStartup args)
    {
        _item.VisualsChanged(ent);
    }

    private void OnShutdown(Entity<FoldedClothingVisualsComponent> ent, ref ComponentShutdown args)
    {
        _item.VisualsChanged(ent);
    }

    private void OnFoldableShutdown(Entity<FoldableComponent> ent, ref ComponentShutdown args)
    {
        if (HasComp<FoldedClothingVisualsComponent>(ent))
            _item.VisualsChanged(ent);
    }
}
