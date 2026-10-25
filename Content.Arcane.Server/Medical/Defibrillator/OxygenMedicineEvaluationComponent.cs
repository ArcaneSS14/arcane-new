namespace Content.Arcane.Server.Medical.Defibrillator;

/// <summary>
/// Adds scoped condition overrides during a shock's medicine evaluation without changing the patient's real state.
/// </summary>
[RegisterComponent]
public sealed partial class OxygenMedicineEvaluationComponent : Component;
