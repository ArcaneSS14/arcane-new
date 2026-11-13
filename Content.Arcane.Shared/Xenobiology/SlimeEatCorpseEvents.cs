using Content.Shared.Actions;
using Content.Shared.DoAfter;

namespace Content.Arcane.Shared.Xenobiology;

public sealed partial class SlimeEatCorpseEvent : EntityTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class SlimeEatCorpseDoAfterEvent : SimpleDoAfterEvent;
