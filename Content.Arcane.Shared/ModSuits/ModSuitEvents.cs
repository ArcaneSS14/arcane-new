using Content.Shared.Actions;

namespace Content.Arcane.Shared.ModSuits;

public sealed partial class ModSuitActivateActionEvent : InstantActionEvent;

public sealed partial class ModSuitDeployActionEvent : InstantActionEvent;

public sealed partial class ModModuleActionEvent : InstantActionEvent;

public sealed partial class ModSelectDeviceActionEvent : InstantActionEvent;

[ByRefEvent]
public readonly record struct ModModuleInstalledEvent(EntityUid Suit);

[ByRefEvent]
public readonly record struct ModModuleRemovedEvent(EntityUid Suit);

[ByRefEvent]
public readonly record struct ModModuleWornEvent(EntityUid Wearer);

[ByRefEvent]
public readonly record struct ModModuleActivatedEvent(EntityUid Suit, EntityUid? Wearer);

[ByRefEvent]
public readonly record struct ModModuleDeactivatedEvent(EntityUid Suit, EntityUid? Wearer);

[ByRefEvent]
public record struct ModModuleUsedEvent(EntityUid Suit, EntityUid User, BaseActionEvent? Action = null)
{
    public bool Handled;
}

[ByRefEvent]
public record struct ModModuleActivateAttemptEvent(EntityUid Suit, EntityUid User)
{
    public bool Cancelled;
}

[ByRefEvent]
public record struct ModSuitAccessAttemptEvent(EntityUid User)
{
    public bool Cancelled;
}

[ByRefEvent]
public record struct ModSpherePressureQuery(float Pressure);

[ByRefEvent]
public readonly record struct ModSuitBatteryChangedEvent(EntityUid Wearer, EntityUid? Battery);

[ByRefEvent]
public record struct ModSuitUseItemEvent(EntityUid User, EntityUid Item, bool Handled = false);
