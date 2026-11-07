using Content.Shared.Interaction;

namespace Content.Server.Construction;

/// <summary>
/// Allows the part-exchanger module to handle an interaction before frame assembly.
/// </summary>
[ByRefEvent]
public record struct MachinePartExchangeRequest(EntityUid Target, InteractUsingEvent Interaction, bool Handled = false);
