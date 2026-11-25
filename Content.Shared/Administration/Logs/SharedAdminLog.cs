using Content.Shared.Database;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration.Logs;

[Serializable, NetSerializable]
public readonly record struct SharedAdminLog(
    int Id,
    LogType Type,
    LogImpact Impact,
    DateTime Date,
    string Message,
    // Arcane-Edit-Start
    Guid[] Players,
    int RoundId = 0);
    // Arcane-Edit-End
