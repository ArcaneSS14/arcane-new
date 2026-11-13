using Content.Shared.Actions;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.StationAi;

public sealed partial class StationAiRetireEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed class ConfirmStationAiRetireMessage : EuiMessageBase;
