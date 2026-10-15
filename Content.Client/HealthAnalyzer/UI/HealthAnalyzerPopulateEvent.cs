// Arcane-Start
using Robust.Client.UserInterface.Controls;

namespace Content.Client.HealthAnalyzer.UI;

/// <summary>Allows client modules to append diagnostics for the patient being scanned.</summary>
[ByRefEvent]
public readonly record struct HealthAnalyzerPopulateEvent(Label Pulse, BoxContainer Conditions);
// Arcane-End
