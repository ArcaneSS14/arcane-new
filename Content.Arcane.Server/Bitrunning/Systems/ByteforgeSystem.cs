using System.Linq;
using Content.Trauma.Common.Effects;
using Content.Server.Storage.EntitySystems;
using Content.Arcane.Shared.Bitrunning;
using Content.Arcane.Shared.Bitrunning.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.EntityTable;
using Content.Shared.Power;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Bitrunning.Systems;

public sealed partial class ByteforgeSystem : EntitySystem
{
    [Dependency] private BitrunningDomainSystem _domains = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private StorageSystem _storage = default!;
    [Dependency] private EntityStorageSystem _entityStorage = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private CommonSparksSystem _sparks = default!;

    private const string ServerSourcePort = "BitrunningServerSource";
    private const string ByteforgeSinkPort = "BitrunningByteforgeSink";

    public override void Initialize()
    {
        SubscribeLocalEvent<ByteforgeComponent, MapInitEvent>(OnByteforgeMapInit);
        SubscribeLocalEvent<ByteforgeComponent, PowerChangedEvent>(OnByteforgePowerChanged);
        SubscribeLocalEvent<QuantumServerComponent, NewLinkEvent>(OnServerNewLink);
        SubscribeLocalEvent<QuantumServerComponent, PortDisconnectedEvent>(OnServerPortDisconnected);
        SubscribeLocalEvent<QuantumServerComponent, GotEmaggedEvent>(OnServerEmagged);
    }

    private void OnByteforgeMapInit(Entity<ByteforgeComponent> ent, ref MapInitEvent args)
    {
        _appearance.SetData(ent, ByteforgeVisuals.ByteforgePowered, _power.IsPowered(ent.Owner));
        _appearance.SetData(ent, ByteforgeVisuals.ByteforgeActive, false);
        _appearance.SetData(ent, ByteforgeVisuals.ByteforgeAngry, IsLinkedServerEmagged(ent.Comp));
    }

    private void OnByteforgePowerChanged(Entity<ByteforgeComponent> ent, ref PowerChangedEvent args)
    {
        _appearance.SetData(ent, ByteforgeVisuals.ByteforgePowered, args.Powered);
    }

    private void OnServerEmagged(Entity<QuantumServerComponent> ent, ref GotEmaggedEvent args)
    {
        args.Handled = true;
        UpdateByteforgeEmagVisual(ent.Comp);
    }

    private void OnServerNewLink(Entity<QuantumServerComponent> ent, ref NewLinkEvent args)
    {
        if (args.Source != ent.Owner || args.SourcePort != ServerSourcePort || args.SinkPort != ByteforgeSinkPort)
            return;

        if (!TryComp<ByteforgeComponent>(args.Sink, out var byteforge))
            return;

        LinkByteforge(ent, (args.Sink, byteforge));
    }

    private void OnServerPortDisconnected(Entity<QuantumServerComponent> ent, ref PortDisconnectedEvent args)
    {
        if (args.Port != ServerSourcePort || ent.Comp.LinkedByteforge is not { } linked)
            return;

        if (args.RemovedPortUid != null && args.RemovedPortUid != linked)
            return;

        UnlinkByteforge(ent, linked);
    }

    /// <summary>
    /// A server delivers to one byteforge and a byteforge serves one server: linking either end
    /// takes it away from its previous partner on both sides.
    /// </summary>
    private void LinkByteforge(Entity<QuantumServerComponent> server, Entity<ByteforgeComponent> byteforge)
    {
        if (server.Comp.LinkedByteforge is { } oldByteforge && oldByteforge != byteforge.Owner)
            UnlinkByteforge(server, oldByteforge);

        if (byteforge.Comp.LinkedServer is { } oldServer && oldServer != server.Owner &&
            TryComp<QuantumServerComponent>(oldServer, out var oldServerComp) && oldServerComp.LinkedByteforge == byteforge.Owner)
        {
            oldServerComp.LinkedByteforge = null;
            Dirty(oldServer, oldServerComp);
        }

        server.Comp.LinkedByteforge = byteforge.Owner;
        byteforge.Comp.LinkedServer = server.Owner;
        UpdateByteforgeEmagVisual(server.Comp);
        Dirty(server);
    }

    /// <summary>
    /// Ends the server's side of the link; the byteforge is only reset if it still belongs to this server.
    /// </summary>
    private void UnlinkByteforge(Entity<QuantumServerComponent> server, EntityUid byteforgeUid)
    {
        if (TryComp<ByteforgeComponent>(byteforgeUid, out var byteforge) && byteforge.LinkedServer == server.Owner)
        {
            byteforge.LinkedServer = null;
            _appearance.SetData(byteforgeUid, ByteforgeVisuals.ByteforgeAngry, false);
        }

        if (server.Comp.LinkedByteforge == byteforgeUid)
            server.Comp.LinkedByteforge = null;

        Dirty(server);
    }

    public bool HasLinkedByteforge(EntityUid serverUid, QuantumServerComponent server)
    {
        if (server.LinkedByteforge is not { } byteforgeUid || !Exists(byteforgeUid))
            return false;

        return TryComp<ByteforgeComponent>(byteforgeUid, out var byteforge) && byteforge.LinkedServer == serverUid;
    }

    public bool TryDeliverObjectiveCargoToByteforge(EntityUid serverUid, EntityUid cargoUid, bool objectiveDone)
    {
        if (!TryComp<QuantumServerComponent>(serverUid, out var server))
            return false;

        if (HasComp<BitrunningDeliveredObjectiveCargoComponent>(cargoUid))
            return false;

        if (!HasLinkedByteforge(serverUid, server))
            return false;

        var byteforgeUid = server.LinkedByteforge!.Value;
        if (!TryComp(byteforgeUid, out TransformComponent? byteforgeXform))
            return false;

        if (!_prototype.HasIndex<EntityPrototype>(server.RewardCachePrototype))
        {
            Log.Warning($"Invalid reward cache prototype '{server.RewardCachePrototype}' on server {ToPrettyString(serverUid)}.");
            return false;
        }

        var rewardCargoUid = Spawn(server.RewardCachePrototype, byteforgeXform.Coordinates);
        _sparks.DoSparks(byteforgeXform.Coordinates);

        if (!TryFillRewardCacheWithLoot(rewardCargoUid, server))
        {
            Log.Warning($"Failed to fill delivered cargo reward crate for server {ToPrettyString(serverUid)}.");
            QueueDel(rewardCargoUid);
            return false;
        }

        if (objectiveDone)
            AddCompletionLoot(rewardCargoUid, server);

        EnsureComp<BitrunningDeliveredObjectiveCargoComponent>(cargoUid);
        PulseByteforge(byteforgeUid);
        QueueDel(cargoUid);
        return true;
    }

    private void PulseByteforge(EntityUid byteforgeUid)
    {
        if (!TryComp<ByteforgeComponent>(byteforgeUid, out var byteforge))
            return;

        byteforge.VisualPulseSerial++;
        var pulseSerial = byteforge.VisualPulseSerial;

        _appearance.SetData(byteforgeUid, ByteforgeVisuals.ByteforgeAngry, IsLinkedServerEmagged(byteforge));
        _appearance.SetData(byteforgeUid, ByteforgeVisuals.ByteforgeActive, true);

        Timer.Spawn(TimeSpan.FromSeconds(1.4f),
            () =>
        {
            if (!TryComp<ByteforgeComponent>(byteforgeUid, out var byteforgeComp) || byteforgeComp.VisualPulseSerial != pulseSerial)
                return;

            _appearance.SetData(byteforgeUid, ByteforgeVisuals.ByteforgeActive, false);
        });
    }

    private bool IsLinkedServerEmagged(ByteforgeComponent byteforge)
    {
        return byteforge.LinkedServer is { } serverUid && HasComp<EmaggedComponent>(serverUid);
    }

    private void UpdateByteforgeEmagVisual(QuantumServerComponent server)
    {
        if (server.LinkedByteforge is not { } byteforgeUid || !Exists(byteforgeUid) || !TryComp<ByteforgeComponent>(byteforgeUid, out var byteforge))
            return;

        _appearance.SetData(byteforgeUid, ByteforgeVisuals.ByteforgeAngry, IsLinkedServerEmagged(byteforge));
    }

    public void RefreshLinkedByteforge(Entity<QuantumServerComponent> ent)
    {
        if (ent.Comp.LinkedByteforge is { } previous)
            UnlinkByteforge(ent, previous);

        if (!TryComp<DeviceLinkSourceComponent>(ent.Owner, out var source))
            return;

        foreach (var outputs in source.Outputs.Values)
        {
            foreach (var linkedEntity in outputs)
            {
                if (!TryComp<ByteforgeComponent>(linkedEntity, out var byteforge))
                    continue;

                // A byteforge another live server already owns stays with it.
                if (byteforge.LinkedServer is { } owner && owner != ent.Owner &&
                    TryComp<QuantumServerComponent>(owner, out var ownerComp) && ownerComp.LinkedByteforge == linkedEntity)
                {
                    continue;
                }

                LinkByteforge(ent, (linkedEntity, byteforge));
                return;
            }
        }
    }

    public bool TryFillRewardCacheWithLoot(EntityUid cargoUid, QuantumServerComponent server)
    {
        // The default completion cache is a virtual objective token without storage: nothing to spawn loot into,
        // the physical crate gets its own fill when it is delivered.
        if (!HasComp<StorageComponent>(cargoUid) && !HasComp<EntityStorageComponent>(cargoUid))
            return false;

        var tableId = GetDifficultyLootTable(server);
        if (!_prototype.TryIndex(tableId, out var table))
            return false;

        var coordinates = Transform(cargoUid).Coordinates;
        var insertedAny = false;
        foreach (var prototypeId in _entityTable.GetSpawns(table))
        {
            var loot = Spawn(prototypeId, coordinates);

            if (TryInsertLoot(cargoUid, loot))
            {
                insertedAny = true;
                continue;
            }

            QueueDel(loot);
        }

        return insertedAny;
    }

    private bool TryInsertLoot(EntityUid cargoUid, EntityUid loot)
    {
        return TryComp<StorageComponent>(cargoUid, out var storage) &&
               _storage.Insert(cargoUid, loot, out _, storageComp: storage, playSound: false) ||
               TryComp<EntityStorageComponent>(cargoUid, out var entityStorage) &&
               _entityStorage.Insert(loot, cargoUid, entityStorage);
    }

    /// <summary>
    /// Fixed loot of the run's domain, once per run. It was earned, so what does not fit stays next to the crate.
    /// </summary>
    private void AddCompletionLoot(EntityUid crate, QuantumServerComponent server)
    {
        if (server.CompletionLootDelivered || server.CurrentDomain == null || !_domains.TryGetDomain(server.CurrentDomain, out var domain))
            return;

        server.CompletionLootDelivered = true;

        var coordinates = Transform(crate).Coordinates;
        foreach (var (prototype, amount) in domain.CompletionLoot)
        {
            for (var i = 0; i < amount; i++)
            {
                TryInsertLoot(crate, Spawn(prototype, coordinates));
            }
        }
    }

    private ProtoId<EntityTablePrototype> GetDifficultyLootTable(QuantumServerComponent server)
    {
        if (server.CurrentDomain == null || !_domains.TryGetDomain(server.CurrentDomain, out var domain))
            return server.DeliveryEasyLootTable;

        var rewardDifficulty = domain.RewardLootDifficulty ?? domain.Difficulty;
        return rewardDifficulty switch
        {
            BitrunningDifficulty.Peaceful => server.DeliveryPeacefulLootTable,
            BitrunningDifficulty.Easy => server.DeliveryEasyLootTable,
            BitrunningDifficulty.Medium => server.DeliveryMediumLootTable,
            BitrunningDifficulty.Hard => server.DeliveryHardLootTable,
            BitrunningDifficulty.Extreme => server.DeliveryExtremeLootTable,
            _ => server.DeliveryEasyLootTable,
        };
    }
}
