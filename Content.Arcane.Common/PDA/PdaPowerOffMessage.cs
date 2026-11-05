namespace Content.Arcane.Common.PDA;

/// <summary>
/// Closes the PDA interface and turns its screen off.
/// </summary>
[Serializable, NetSerializable]
public sealed class PdaPowerOffMessage : BoundUserInterfaceMessage;
