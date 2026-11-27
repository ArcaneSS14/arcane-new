using System.Linq;
using Content.Arcane.Shared.DiscordRoles;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Arcane.Shared.Sponsor;

/// <summary>
/// The sponsor-only prototypes (markings, species) a player with these Discord roles may use.
/// </summary>
public static class SponsorPrototypeList
{
    public static List<string> For(IPrototypeManager prototypes, Func<DiscordRole, bool> hasRole)
    {
        var anySponsor = SponsorRoleBenefits.All.Keys.Any(hasRole);
        var result = new List<string>();

        foreach (var marking in prototypes.EnumeratePrototypes<MarkingPrototype>())
        {
            if (!marking.SponsorOnly)
                continue;

            var allowed = marking.SponsorRoles.Count == 0
                ? anySponsor
                : marking.SponsorRoles.Any(name => Enum.TryParse<DiscordRole>(name, out var role) && hasRole(role));
            if (allowed)
                result.Add(marking.ID);
        }

        if (anySponsor)
        {
            foreach (var species in prototypes.EnumeratePrototypes<SpeciesPrototype>())
            {
                if (species.SponsorOnly)
                    result.Add(species.ID);
            }
        }

        return result;
    }
}
