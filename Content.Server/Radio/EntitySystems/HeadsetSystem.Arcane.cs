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

    private const SlotFlags EarSlots = SlotFlags.EARS | SlotFlags.EARSRIGHT;

    /// <summary>
    /// Enabled headsets worn in either ear, left ear first.
    /// </summary>
    private IEnumerable<Entity<HeadsetComponent>> GetWornHeadsets(EntityUid wearer)
    {
        var enumerator = _inventory.GetSlotEnumerator(wearer, EarSlots);
        while (enumerator.NextItem(out var item))
        {
            if (TryComp<HeadsetComponent>(item, out var headset) && headset.Enabled && headset.IsEquipped)
                yield return (item, headset);
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
