using Content.Arcane.Shared.ModSuits.Components;
using Content.Client.Clothing;
using Content.Client.Toggleable;
using Content.Goobstation.Client.Clothing.Systems;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Serialization.Manager;

namespace Content.Arcane.Client.ModSuits;

public sealed partial class ModHatVisualsSystem : EntitySystem
{
    [Dependency] private ISerializationManager _serialization = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;

    [SubscribeLocalEvent(after: [typeof(ClientClothingSystem), typeof(SealableClothingVisualizerSystem), typeof(ToggleableVisualsSystem)])]
    private void OnGetEquipmentVisuals(EntityUid uid, ModHatHolderComponent component, GetEquipmentVisualsEvent args)
    {
        if (_itemSlots.GetItemOrNull(uid, ModHatHolderComponent.SlotId) is not { } hat)
            return;

        var hatVisuals = new GetEquipmentVisualsEvent(args.Equipee, args.Slot);
        RaiseLocalEvent(hat, hatVisuals);
        var rsiPath = CompOrNull<ClothingComponent>(hat)?.RsiPath
            ?? CompOrNull<SpriteComponent>(hat)?.BaseRSI?.Path.ToString();

        foreach (var (key, layer) in hatVisuals.Layers)
        {
            var data = _serialization.CreateCopy(layer, notNullableOverride: true);
            if (data.RsiPath == null && data.TexturePath == null)
                data.RsiPath = rsiPath;

            var hatKey = $"mod-hat-{key}";
            args.Layers.Add((hatKey, data));
            if (hatVisuals.LayersAnimationTime.TryGetValue(key, out var time))
                args.LayersAnimationTime[hatKey] = time;
        }
    }
}
