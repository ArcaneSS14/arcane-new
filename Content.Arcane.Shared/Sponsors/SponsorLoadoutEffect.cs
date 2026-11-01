using System.Diagnostics.CodeAnalysis;
using Content.Arcane.Common.Sponsors;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Preferences.Loadouts.Effects;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Arcane.Shared.Sponsors;

/// <summary>
/// Only sponsors that own the given loadout prototype can select it.
/// </summary>
public sealed partial class SponsorLoadoutEffect : LoadoutEffect
{
    public override bool Validate(HumanoidCharacterProfile profile,
        RoleLoadout loadout,
        LoadoutPrototype proto,
        ICommonSession? session,
        IDependencyCollection collection,
        [NotNullWhen(false)] out FormattedMessage? reason)
    {
        reason = null;

        if (session == null)
            return true;

        var sponsorProtos = SponsorPrototypes.Get(session, collection);
        if (!sponsorProtos.Contains(proto.ID))
        {
            reason = FormattedMessage.FromMarkupOrThrow(Loc.GetString("loadout-sponsor-only"));
            return false;
        }

        return true;
    }
}
