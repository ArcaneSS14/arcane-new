using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.Actions;

/// <summary>
/// Bounded, unique hotbar positions shared by the optimistic client and authoritative server.
/// Moving a pin onto another pin swaps their positions.
/// </summary>
public static class PinnedActionPositions
{
    public const int MaxPins = 32;
    public const int MaxSlots = 256;

    public static int FirstFree(IReadOnlyDictionary<EntProtoId, int> pins)
    {
        for (var i = 0; i < MaxSlots; i++)
        {
            if (!pins.Values.Contains(i))
                return i;
        }
        return -1;
    }

    public static bool Place(Dictionary<EntProtoId, int> pins, EntProtoId prototype, int slot)
    {
        if (slot < 0 || slot >= MaxSlots)
            return false;

        var previous = pins.TryGetValue(prototype, out var oldSlot) ? oldSlot : FirstFree(pins);
        if (previous < 0)
            return false;

        EntProtoId? occupied = null;
        foreach (var (other, position) in pins)
        {
            if (other != prototype && position == slot)
            {
                occupied = other;
                break;
            }
        }
        if (occupied is { } otherPrototype)
            pins[otherPrototype] = previous;
        pins[prototype] = slot;
        return true;
    }
}
