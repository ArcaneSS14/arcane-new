// MIT License

// Copyright (c) 2026 Vecortys (vecortys@gmail.com)

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using Content.Shared.AlertLevel;
using Content.Server.Chat.Systems;
using Content.Shared.Station.Systems;
using Content.Trauma.Shared.Effects;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.InfinityDorm;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Chat;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Events;
using Robust.Shared.Configuration;
using Robust.Shared.Map;

namespace Content.Arcane.Server.InfinityDorm;

public sealed partial class InfinityDormSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private AlertLevelSystem _alertLevel = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private AccessReaderSystem _accessReader = default!;
    [Dependency] private SparksSystem _sparks = default!;
    [Dependency] private ChatSystem _chat = default!;

    private int _maxUserDorms = 0;

    private int _step = 300;
    private float _lastPosition;
    private MapId _dormsMapId;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InfinityDormTeleporterComponent, InfinityDormTeleportMessage>(HandleTeleporterMessage);
        SubscribeNetworkEvent<RequestDormsAmountEvent>(HandleDormsAmountRequest);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarted);

        Subs.CVar(_cfg, ArcaneCVars.MaxUserInfinityDorms, SetMaxUserDorms, true);
    }

    private void HandleTeleporterMessage(EntityUid uid, InfinityDormTeleporterComponent component, InfinityDormTeleportMessage args)
    {
        if (!_cfg.GetCVar(ArcaneCVars.InfinityDormsEnabled))
        {
            _chat.TrySendInGameICMessage(uid, Loc.GetString("infinity-dorm-warning-disabled"), InGameICChatType.Speak, false);
            return;
        }

        var station = _station.GetOwningStation(uid);
        if (station != null && _alertLevel.TryGetLevel(station.Value, out var level) && level != component.NeedAlertLevel)
        {
            _chat.TrySendInGameICMessage(uid, Loc.GetString("infinity-dorm-warning-alert-level"), InGameICChatType.Speak, false);
            return;
        }

        EnsureDormsMap();
        if (!TryCreateDorm(uid, args.Actor, args.Room, args.Number))
            return;

        var accessReader = EnsureComp<AccessReaderComponent>(uid);
        _accessReader.LogAccess((uid, accessReader), args.Actor);

        _sparks.DoSparks(Transform(args.Actor).Coordinates, args.Actor, 3, 10);
        TeleportToDorm(args.Actor, args.Number);
        _sparks.DoSparks(Transform(args.Actor).Coordinates, args.Actor, 3, 10);
    }

    private void HandleDormsAmountRequest(RequestDormsAmountEvent args, EntitySessionEventArgs eventArgs)
    {
        var count = 0;
        var query = EntityQueryEnumerator<InfinityDormComponent>();

        while (query.MoveNext(out var _, out var comp))
        {
            if (comp.Creator == eventArgs.SenderSession.AttachedEntity)
                count++;
        }

        RaiseNetworkEvent(new UserDormCountMessage(count), eventArgs.SenderSession);
    }

    private void OnRoundStarted(RoundStartingEvent args)
    {
        _dormsMapId = MapId.Nullspace;
        _lastPosition = -1000;
    }

    private void EnsureDormsMap()
    {
        if (_map.MapExists(_dormsMapId))
            return;

        _map.CreateMap(out _dormsMapId);
    }

    private void SetMaxUserDorms(int value)
    {
        _maxUserDorms = value;
    }

}
