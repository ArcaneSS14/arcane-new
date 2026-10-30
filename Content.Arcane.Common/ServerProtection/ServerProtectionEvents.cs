using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Arcane.Common.ServerProtection;

/// <summary>
///     Raised broadcast when an entity sends an emote to chat.
/// </summary>
[ByRefEvent]
public readonly record struct EntityEmoteSentEvent(EntityUid Source);

/// <summary>
///     Raised broadcast when an admin places a server ban.
/// </summary>
[ByRefEvent]
public readonly record struct AdminBanPlacedEvent(NetUserId Admin, string AdminName, string TargetName);

/// <summary>
///     Raised broadcast when an admin removes another admin or reduces their permissions.
/// </summary>
[ByRefEvent]
public readonly record struct AdminPermissionsReducedEvent(NetUserId Admin, string AdminName, string TargetName, string Action);

/// <summary>
///     Raised broadcast when a protection cvar is changed with the cvar command.
/// </summary>
[ByRefEvent]
public readonly record struct ProtectionCVarChangedEvent(string CVar, ICommonSession? Actor, object? OldValue, object? NewValue);
