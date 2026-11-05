using Robust.Shared.Network;

namespace Content.Arcane.Common.Ghost;

/// <summary>
/// Raised broadcast before an observer ghost is spawned for a player, so the prototype can be swapped
/// for a custom ghost. <see cref="Custom"/> is set when it was, to skip death-cause sprites the custom one lacks.
/// </summary>
[ByRefEvent]
public record struct GhostPrototypeEvent(NetUserId? User, string Prototype, bool Custom = false);
