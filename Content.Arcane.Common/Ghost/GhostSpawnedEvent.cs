namespace Content.Arcane.Common.Ghost;

[ByRefEvent]
public readonly record struct GhostSpawnedEvent(EntityUid Ghost, NetUserId? User);
