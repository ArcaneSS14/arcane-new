using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Arcane.Shared.Species.Hydrakin;

public sealed partial class HydrakinCoolOffActionEvent : InstantActionEvent { }

[Serializable, NetSerializable]
public sealed partial class HydrakinCoolOffDoAfterEvent : SimpleDoAfterEvent { }
