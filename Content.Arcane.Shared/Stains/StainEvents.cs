using Content.Shared.Chemistry.Components;
using Content.Shared.Inventory;

namespace Content.Arcane.Shared.Stains;

/// <summary>
/// Raised on a mob and relayed to what it wears when it slips on something that can stain clothes.
/// </summary>
public sealed class StainSlippedEvent(Entity<SolutionComponent> source) : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;

    /// <summary>
    /// The solution of what was slipped on; stains take from it and cleaning reagents in it wash them.
    /// </summary>
    public readonly Entity<SolutionComponent> Source = source;
}

/// <summary>
/// Raised on a mob and relayed to what it wears when a solution touches it: splashes, sprays, crawling or
/// stepping through a puddle.
/// </summary>
public sealed class StainSpilledOnEvent(Solution solution) : EntityEventArgs, IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;

    public readonly Solution Solution = solution;
}
