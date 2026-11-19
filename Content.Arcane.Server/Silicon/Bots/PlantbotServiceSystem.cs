using Content.Arcane.Shared.Silicon.Bots;
using Content.Server.Chat.Systems;
using Content.Server.NPC.HTN;
using Content.Shared.Botany.Components;
using Content.Shared.Botany.Systems;
using Content.Shared.Chat;
using Content.Shared.Emag.Systems;
using Content.Shared.Interaction;
using Robust.Shared.Audio.Systems;

namespace Content.Arcane.Server.Silicon.Bots;

/// <summary>
/// Performs one server-authoritative service action through Trauma's tray API.
/// The same eligibility check is used during selection and after the HTN wait.
/// </summary>
public sealed partial class PlantbotServiceSystem : EntitySystem
{
    [Dependency] private PlantTraySystem _trays = default!;
    [Dependency] private EmagSystem _emag = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private HTNSystem _htn = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PlantbotComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnShutdown(Entity<PlantbotComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent) && !EntityManager.IsQueuedForDeletion(ent) &&
            TryComp<HTNComponent>(ent, out var htn))
            _htn.SetHTNEnabled((ent.Owner, htn), false);
    }

    private enum Service
    {
        None,
        Water,
        Weed,
        Drain,
    }

    public bool CanService(EntityUid owner, EntityUid target)
        => TryComp<PlantbotComponent>(owner, out var bot) &&
           TryComp<PlantTrayComponent>(target, out var tray) &&
           GetService(owner, bot, (target, tray)) != Service.None;

    private Service GetService(EntityUid owner, PlantbotComponent bot, Entity<PlantTrayComponent> tray)
    {
        if (TerminatingOrDeleted(owner) || TerminatingOrDeleted(tray) ||
            EntityManager.IsQueuedForDeletion(owner) || EntityManager.IsQueuedForDeletion(tray) ||
            Transform(owner).MapID != Transform(tray).MapID)
            return Service.None;

        if (_emag.CheckFlag(owner, EmagType.Interaction))
        {
            if (bot.WaterAmount <= 0 || tray.Comp.WaterLevel <= 0)
                return Service.None;

            if (_trays.TryGetPlant(tray.AsNullable(), out var plant) &&
                TryComp<PlantHolderComponent>(plant, out var holder) && holder.Dead)
                return Service.None;

            return Service.Drain;
        }

        if (bot.WaterAmount > 0 && tray.Comp.WaterLevel < Math.Min(bot.WaterThreshold, tray.Comp.MaxWaterLevel))
            return Service.Water;

        if (bot.WeedAmount > 0 && tray.Comp.WeedCoefficient > 0 && tray.Comp.WeedLevel > bot.WeedThreshold)
            return Service.Weed;

        return Service.None;
    }

    public bool TryService(EntityUid owner, EntityUid target)
    {
        if (!TryComp<PlantbotComponent>(owner, out var bot) ||
            !TryComp<PlantTrayComponent>(target, out var tray) ||
            !_interaction.InRangeAndAccessible(owner, target))
            return false;

        switch (GetService(owner, bot, (target, tray)))
        {
            case Service.Water:
            {
                var oldWater = tray.WaterLevel;
                _trays.AdjustWater((target, tray), bot.WaterAmount);
                if (tray.WaterLevel <= oldWater)
                    return false;

                _audio.PlayPvs(bot.WaterSound, target);
                _chat.TrySendInGameICMessage(owner, Loc.GetString("plantbot-add-water"),
                    InGameICChatType.Speak, hideChat: true, hideLog: true);
                return true;
            }
            case Service.Weed:
            {
                var oldWeeds = tray.WeedLevel;
                _trays.AdjustWeed((target, tray), -bot.WeedAmount);
                if (tray.WeedLevel >= oldWeeds)
                    return false;

                _audio.PlayPvs(bot.WeedSound, target);
                _chat.TrySendInGameICMessage(owner, Loc.GetString("plantbot-remove-weeds"),
                    InGameICChatType.Speak, hideChat: true, hideLog: true);
                return true;
            }
            case Service.Drain:
            {
                var oldWater = tray.WaterLevel;
                _trays.AdjustWater((target, tray), -bot.WaterAmount);
                if (tray.WaterLevel >= oldWater)
                    return false;

                _audio.PlayPvs(bot.RemoveWaterSound, target);
                return true;
            }
            default:
                return false;
        }
    }
}
