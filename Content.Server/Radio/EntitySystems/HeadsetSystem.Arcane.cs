using Content.Shared.Inventory;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;

namespace Content.Server.Radio.EntitySystems;

/// <summary>
/// Arcane - headsets in both ears (Orion #208).
/// </summary>
public sealed partial class HeadsetSystem
{
    [Dependency] private InventorySystem _inventory = default!;

    /// <summary>
    /// Enabled headsets worn in a slot they work in (the same check as equipping), the ones in their required
    /// slot first, so the left ear stays the main headset.
    /// </summary>
    private IEnumerable<Entity<HeadsetComponent>> GetWornHeadsets(EntityUid wearer)
    {
        var additional = new List<Entity<HeadsetComponent>>();
        var enumerator = _inventory.GetSlotEnumerator(wearer);
        while (enumerator.NextItem(out var item, out var slot))
        {
            var slotFlags = slot.SlotFlags;
            if (!TryComp<HeadsetComponent>(item, out var headset) || !headset.Enabled || !IsHeadsetSlot(headset, slotFlags))
                continue;

            if ((slotFlags & headset.RequiredSlot) != SlotFlags.NONE)
                yield return (item, headset);
            else
                additional.Add((item, headset));
        }

        foreach (var headset in additional)
        {
            yield return headset;
        }
    }

    /// <summary>
    /// Points <see cref="WearingHeadsetComponent"/> at the first worn headset, or removes it if there is none.
    /// </summary>
    private void UpdateWearingHeadset(EntityUid wearer)
    {
        foreach (var headset in GetWornHeadsets(wearer))
        {
            EnsureComp<WearingHeadsetComponent>(wearer).Headset = headset;
            return;
        }

        RemComp<WearingHeadsetComponent>(wearer);
    }

    /// <summary>
    /// When both headsets hear a channel, only the main one delivers the message.
    /// </summary>
    private bool IsDuplicateReceive(EntityUid headset, EntityUid wearer, RadioChannelPrototype channel)
    {
        return TryComp<WearingHeadsetComponent>(wearer, out var wearing)
               && wearing.Headset != headset
               && TryComp<ActiveRadioComponent>(wearing.Headset, out var main)
               && main.Channels.Contains(channel.ID);
    }
}
