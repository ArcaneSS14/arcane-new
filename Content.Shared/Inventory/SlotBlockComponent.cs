using Robust.Shared.GameStates;

namespace Content.Shared.Inventory;

/// <summary>
/// Used to prevent items from being unequipped and equipped from slots that are listed in <see cref="Slots"/>.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SlotBlockSystem))]
public sealed partial class SlotBlockComponent : Component
{
    /// <summary>
    /// Slots that this entity should block.
    /// </summary>
    [DataField, AutoNetworkedField] // Arcane-Edit: no longer required, a blocker can only hide slots
    public SlotFlags Slots = SlotFlags.NONE;

    // Arcane-Start
    /// <summary>
    /// Slots that stay usable but whose contents are hidden from others in the strip menu.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SlotFlags HiddenSlots = SlotFlags.NONE;
    // Arcane-End
}
