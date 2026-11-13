using Content.Shared.Containers.ItemSlots;
using Robust.Shared.GameStates;

namespace Content.Arcane.Shared.Security;

[RegisterComponent, NetworkedComponent, Access(typeof(SidearmFlashlightMountSystem))]
public sealed partial class SidearmFlashlightMountComponent : Component
{
    public const string SlotId = "flashlight";

    [DataField(required: true)]
    public ItemSlot Slot = new();
}
