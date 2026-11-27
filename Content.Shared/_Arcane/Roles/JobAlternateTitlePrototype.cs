// Arcane-Start
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Shared.Roles;

/// <summary>
/// A title a player can use for a job instead of its name; see <see cref="JobPrototype.AlternateTitles"/>.
/// </summary>
[Prototype]
public sealed partial class JobAlternateTitlePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Title shown for any gender without its own variant.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Title for female characters, if it differs.
    /// </summary>
    [DataField]
    public LocId? FemaleName;

    /// <summary>
    /// Title for male characters, if it differs.
    /// </summary>
    [DataField]
    public LocId? MaleName;

    /// <summary>
    /// Requirements to use this title, on top of the job's own.
    /// </summary>
    [DataField]
    public HashSet<JobRequirement>? Requirements;

    public string LocalizedName(Gender? gender)
    {
        return gender switch
        {
            Gender.Female => Loc.GetString(FemaleName ?? Name),
            Gender.Male => Loc.GetString(MaleName ?? Name),
            _ => Loc.GetString(Name),
        };
    }
}
// Arcane-End
