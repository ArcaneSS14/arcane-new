using Content.Arcane.Shared.Security;
using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Shared.Access;
using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Content.Shared.Station.Systems;
using Content.Shared.Timing.Systems;
using Robust.Shared.Utility;

namespace Content.Arcane.Server.Security;

public sealed partial class PanicButtonSystem : EntitySystem
{
    [Dependency] private AccessReaderSystem _access = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private NavMapSystem _navigation = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private UseDelaySystem _useDelay = default!;

    private static readonly ProtoId<AccessLevelPrototype> SecurityAccess = "Security";
    private static readonly ProtoId<RadioChannelPrototype> SecurityChannel = "Security";

    [SubscribeLocalEvent]
    private void OnUse(Entity<PanicButtonComponent> ent, ref UseInHandEvent args)
    {
        var user = args.User;
        if (args.Handled || !_hands.IsHolding(user, ent) || !_actionBlocker.CanInteract(user, ent))
            return;

        args.Handled = true;
        args.ApplyDelay = false;
        if (!_access.FindAccessTags(user).Contains(SecurityAccess))
        {
            _popup.PopupEntity(Loc.GetString("panic-button-no-access"), ent, user);
            return;
        }

        if (_station.GetOwningStation(user) == null)
        {
            _popup.PopupEntity(Loc.GetString("panic-button-no-station"), ent, user);
            return;
        }

        var location = FormattedMessage.RemoveMarkupPermissive(_navigation.GetNearestBeaconString(user));
        if (!_useDelay.TryResetDelay(ent.Owner, checkDelayed: true))
            return;

        _radio.SendRadioMessage(user, Loc.GetString("panic-button-distress", ("position", location)),
            ProtoMan.Index(SecurityChannel), ent);
    }

    [SubscribeLocalEvent]
    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent args)
    {
        if (args.Cancelled || !HasComp<PanicButtonComponent>(args.RadioSource))
            return;

        var station = _station.GetOwningStation(args.RadioSource);
        args.Cancelled = station == null || _station.GetOwningStation(args.RadioReceiver) != station;
    }
}
