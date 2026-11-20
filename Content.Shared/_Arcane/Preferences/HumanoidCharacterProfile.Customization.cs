using System.Linq;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Common.Preferences;
using Content.Shared.CCVar;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    /// <summary>
    /// Custom species name and hair gradient.
    /// </summary>
    [DataField]
    public CharacterCustomization Customization { get; private set; } = new();

    public HumanoidCharacterProfile WithCustomization(CharacterCustomization customization)
    {
        return new(this) { Customization = customization };
    }

    private CharacterCustomization CustomizationWithoutSpeciesName()
    {
        var copy = Customization.Clone();
        copy.CustomSpeciesName = string.Empty;
        return copy;
    }

    /// <summary>
    /// Customization valid for this profile's species: the custom species name only where the species allows it.
    /// </summary>
    private CharacterCustomization ValidatedCustomization(IPrototypeManager prototypes, IConfigurationManager config)
    {
        var allows = prototypes.TryIndex<SpeciesPrototype>(Species, out var species) && species.CustomName;
        var forbidden = allows && !string.IsNullOrWhiteSpace(Customization.CustomSpeciesName) && config.GetCVar(ArcaneCVars.RestrictedCustomSpeciesNames)
            ? prototypes.EnumeratePrototypes<SpeciesPrototype>().Select(s => Loc.TryGetString(s.Name, out var name) ? name : s.Name).ToList()
            : null;
        return Customization.Validated(allows, config.GetCVar(CCVars.MaxNameLength), forbidden);
    }
}
