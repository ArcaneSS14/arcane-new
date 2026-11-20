using Content.Arcane.Shared.Gambling;
using Content.Shared.Chat;
using Content.Shared.EntityTable;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Arcane.Server.Gambling;

/// <summary>Server-only selection and delivery of a gambling outcome.</summary>
public sealed partial class PrizeSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private EntityTableSystem _tables = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChatSystem _chat = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public PrizePrototype? Pick(List<ProtoId<PrizePrototype>> prizes)
    {
        var sum = 0f;
        foreach (var id in prizes)
            sum += Math.Max(0, _prototypes.Index(id).Weight);
        if (sum <= 0 || !float.IsFinite(sum))
            return null;

        var roll = _random.NextFloat() * sum;
        PrizePrototype? last = null;
        foreach (var id in prizes)
        {
            var prize = _prototypes.Index(id);
            if (prize.Weight <= 0)
                continue;
            last = prize;
            roll -= prize.Weight;
            if (roll < 0)
                return prize;
        }
        return last;
    }

    public IReadOnlyList<EntityUid> Award(PrizePrototype prize, EntityUid machine, EntityUid? user)
    {
        var delivered = new List<EntityUid>();
        var coordinates = Transform(machine).Coordinates;
        foreach (var prototype in _tables.GetSpawns(prize.PrizeTable))
        {
            var item = Spawn(prototype, coordinates);
            if (!TerminatingOrDeleted(item) && !EntityManager.IsQueuedForDeletion(item))
                delivered.Add(item);
        }
        if (delivered.Count > 0 && user is { } recipient
            && !TerminatingOrDeleted(recipient) && !EntityManager.IsQueuedForDeletion(recipient))
        {
            var won = new PrizeWonEvent(prize.ID, machine, delivered[0]);
            RaiseLocalEvent(recipient, ref won);
        }
        if (prize.WinMessage is { } message)
        {
            if (prize.AnnounceType == PrizeAnnounceType.Popup)
                _popup.PopupEntity(Loc.GetString(message), machine);
            else
                _chat.TrySendInGameICMessage(machine, Loc.GetString(message), InGameICChatType.Speak, hideChat: false, hideLog: true, checkRadioPrefix: false);
        }
        if (prize.WinSound != null)
            _audio.PlayPvs(prize.WinSound, machine);
        return delivered;
    }
}
