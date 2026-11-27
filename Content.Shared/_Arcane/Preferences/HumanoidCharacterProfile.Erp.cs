// Arcane-Start
using Content.Arcane.Common.Preferences;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    /// <summary>
    /// Only adults can take part in ERP interactions, whatever the species allows as a minimum age.
    /// </summary>
    public const int ErpMinAge = 18;

    /// <summary>
    /// Whether this character takes part in ERP interactions.
    /// </summary>
    [DataField]
    public ErpPreference ErpPreference { get; private set; } = ErpPreference.Ask;

    public HumanoidCharacterProfile WithErpPreference(ErpPreference preference)
    {
        return new(this) { ErpPreference = preference };
    }

    /// <summary>
    /// The preference with unknown values and characters under <see cref="ErpMinAge"/> turned into
    /// <see cref="ErpPreference.No"/>.
    /// </summary>
    private ErpPreference ValidatedErpPreference(int age)
    {
        if (age < ErpMinAge || !Enum.IsDefined(ErpPreference))
            return ErpPreference.No;

        return ErpPreference;
    }
}
// Arcane-End
