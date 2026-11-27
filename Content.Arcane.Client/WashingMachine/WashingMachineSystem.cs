using Content.Client.Inventory;
using Content.Arcane.Shared.WashingMachine;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Arcane.Client.WashingMachine;

public sealed partial class WashingMachineSystem : SharedWashingMachineSystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private MarkingManager _markingManager = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    private static readonly HumanoidVisualLayers[] HiddenAppearanceLayers =
    [
        HumanoidVisualLayers.Special,
        HumanoidVisualLayers.Hair,
        HumanoidVisualLayers.FacialHair,
        HumanoidVisualLayers.Face,
        HumanoidVisualLayers.Head,
        HumanoidVisualLayers.Snout,
        HumanoidVisualLayers.SnoutCover,
        HumanoidVisualLayers.HeadSide,
        HumanoidVisualLayers.HeadTop,
        HumanoidVisualLayers.Eyes,
        HumanoidVisualLayers.RArm,
        HumanoidVisualLayers.LArm,
        HumanoidVisualLayers.RHand,
        HumanoidVisualLayers.LHand,
        HumanoidVisualLayers.Handcuffs,
    ];

    private static readonly string[] HiddenClothingSlots =
        ["head", "eyes", "ears", "earsright", "mask", "neck"];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WashingMachineStuckComponent, ComponentAdd>(OnStuckAdd);
    }

    private void OnStuckAdd(EntityUid uid, WashingMachineStuckComponent component, ComponentAdd args)
    {
        ApplyHiddenLayers(uid, component, true);
    }

    private List<object> GetHiddenLayerKeys(EntityUid uid, InventorySlotsComponent? inventory)
    {
        var keys = new List<object>(HiddenAppearanceLayers.Length);

        foreach (var visual in HiddenAppearanceLayers)
            keys.Add(visual);

        if (inventory != null)
        {
            foreach (var slot in HiddenClothingSlots)
            {
                if (inventory.VisualLayerKeys.TryGetValue(slot, out var revealed))
                    keys.AddRange(revealed);
            }
        }

        foreach (var organ in _body.EnumerateOrgans<VisualOrganMarkingsComponent>(uid))
            AddMarkingLayerKeys(organ.Comp2, keys);

        return keys;
    }

    private void AddMarkingLayerKeys(VisualOrganMarkingsComponent markings, List<object> keys)
    {
        foreach (var markingList in markings.Markings.Values)
        {
            foreach (var marking in markingList)
            {
                if (!_markingManager.TryGetMarking(marking, out var markerProto) ||
                    !HiddenAppearanceLayers.Contains(markerProto.BodyPart))
                    continue;

                foreach (var spriteSpec in markerProto.Sprites)
                {
                    if (spriteSpec is SpriteSpecifier.Rsi rsi)
                        keys.Add($"{marking.MarkingId}-{rsi.RsiState}");
                }
            }
        }
    }

    private void ApplyHiddenLayers(EntityUid uid, WashingMachineStuckComponent component, bool snapshot)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        TryComp<InventorySlotsComponent>(uid, out var inventory);

        var keys = GetHiddenLayerKeys(uid, inventory);
        foreach (var key in keys)
        {
            if (!TryGetLayerIndex((uid, sprite), key, out var index))
                continue;

            if (snapshot && !component.PreLayerVisibility.ContainsKey(key))
                component.PreLayerVisibility[key] = sprite[index].Visible;

            _sprite.LayerSetVisible((uid, sprite), index, false);
        }
    }

    private bool TryGetLayerIndex(Entity<SpriteComponent?> sprite, object key, out int index)
    {
        if (key is Enum enumKey)
            return _sprite.LayerMapTryGet(sprite, enumKey, out index, false);

        if (key is string strKey)
            return _sprite.LayerMapTryGet(sprite, strKey, out index, false);

        index = 0;
        return false;
    }

    protected override void RestoreStuckVisuals(Entity<WashingMachineStuckComponent> ent)
    {
        base.RestoreStuckVisuals(ent);

        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        TryComp<InventorySlotsComponent>(ent.Owner, out var inventory);

        var keys = GetHiddenLayerKeys(ent.Owner, inventory);
        foreach (var key in keys)
        {
            if (!TryGetLayerIndex((ent.Owner, sprite), key, out var index))
                continue;

            var visible = ent.Comp.PreLayerVisibility.GetValueOrDefault(key, true);
            _sprite.LayerSetVisible((ent.Owner, sprite), index, visible);
        }

        ent.Comp.PreLayerVisibility.Clear();
    }
}