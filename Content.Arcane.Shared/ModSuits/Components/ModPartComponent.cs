using Content.Arcane.Shared.ModSuits.Systems;
using Content.Shared.Clothing.Components;
using Robust.Shared.Containers;

namespace Content.Arcane.Shared.ModSuits.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ModSuitSystem))]
public sealed partial class ModPartComponent : Component
{
    [AutoNetworkedField]
    public EntityUid? Suit;

    [AutoNetworkedField]
    public string Slot = string.Empty;

    [AutoNetworkedField]
    public bool Sealed;

    [DataField]
    public ComponentRegistry? SealedComponents;

    public readonly ComponentRegistry AddedWhileSealed = new();

    public HideLayerClothingComponent? UnsealedLayers;

    public ContainerSlot? StoredClothing;
}
