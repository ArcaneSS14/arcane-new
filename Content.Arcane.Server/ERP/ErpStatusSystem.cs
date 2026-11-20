using Content.Arcane.Common.Preferences;
using Content.Server.Preferences.Managers;
using Content.Arcane.Shared.ERP;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Robust.Shared.Player;

namespace Content.Arcane.Server.ERP;

public sealed partial class ErpStatusSystem : EntitySystem
{
    [Dependency] private IServerPreferencesManager _prefs = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<HumanoidProfileComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnPlayerAttached(Entity<HumanoidProfileComponent> ent, ref PlayerAttachedEvent args)
    {
        var profile = _prefs.GetPreferencesOrNull(args.Player.UserId)?.SelectedCharacter as HumanoidCharacterProfile;
        var preference = profile?.ErpPreference ?? ErpPreference.Ask;
        // The body's age counts, not the profile's: ghost roles and polymorphs have their own.
        if (ent.Comp.Age < HumanoidCharacterProfile.ErpMinAge)
            preference = ErpPreference.No;

        EnsureComp<ArousalComponent>(ent);

        var comp = EnsureComp<ErpStatusComponent>(ent);
        var oldPreference = comp.Preference;
        comp.Preference = preference;
        Dirty(ent, comp);

        if (oldPreference != preference)
            RaiseLocalEvent(ent, new ErpPreferenceChangedEvent(oldPreference, preference));
    }
}
