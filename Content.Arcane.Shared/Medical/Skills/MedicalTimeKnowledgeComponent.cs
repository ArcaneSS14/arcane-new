namespace Content.Arcane.Shared.Medical.Skills;

/// <summary>
/// Knowledge component that scales the time of a medical action with skill level.
/// Read from the skill's prototype, so it also applies to people who have no unit of that skill.
/// </summary>
[RegisterComponent, Access(typeof(MedicalTimeKnowledgeSystem))]
public sealed partial class MedicalTimeKnowledgeComponent : Component
{
    /// <summary>
    /// Time multipliers by skill level: above 1 is slower, below 1 is faster.
    /// Levels between two entries are interpolated linearly.
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<int, float> Multipliers = [];
}
