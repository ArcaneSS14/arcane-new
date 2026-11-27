namespace Content.Arcane.Common.Radio;

/// <summary>
/// Raised on a mob with an intrinsic radio (borgs, the station AI) after it received a radio message,
/// so Arcane systems can react the same way they do for headset wearers.
/// </summary>
[ByRefEvent]
// Frequency is the FixedPoint2 value in hundredths, preserving fractional channels.
public readonly record struct IntrinsicRadioReceivedEvent(EntityUid MessageSource, string Message, string Language, int Frequency);
