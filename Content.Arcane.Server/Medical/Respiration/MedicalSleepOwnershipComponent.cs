namespace Content.Arcane.Server.Medical.Respiration;

/// <summary>
/// Remembers whether the first medical sleep source introduced sleep or joined an existing cause.
/// </summary>
[RegisterComponent]
public sealed partial class MedicalSleepOwnershipComponent : Component
{
    public bool OwnsSleep;
}
