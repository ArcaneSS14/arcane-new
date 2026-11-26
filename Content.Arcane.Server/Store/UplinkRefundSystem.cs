using System.Linq;
using Content.Shared.FixedPoint;
using Content.Server.Store.Systems;
using Content.Server.Traitor.Uplink;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Access.Components;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.Throwing;
using Content.Shared.Trigger.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Misc;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Arcane.Server.Store;

public sealed partial class UplinkRefundSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StoreSystem _store = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    private const float RefundRatio = 0.8f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StoreBuyFinishedEvent>(OnBuyFinished);
    }

    private void OnBuyFinished(ref StoreBuyFinishedEvent args)
    {
        if (!HasComp<UplinkRefundStoreComponent>(args.StoreUid)
            || args.ProductEntity is not { } product || args.PaidCost is not { Count: 1 } cost
            || !cost.TryGetValue(UplinkSystem.TelecrystalCurrencyPrototype, out var amount))
            return;

        var refundable = EnsureComp<UplinkRefundableComponent>(product);
        refundable.OriginalUplink = args.StoreUid;
        refundable.Currency = UplinkSystem.TelecrystalCurrencyPrototype;
        refundable.RefundAmount = FixedPoint2.New((int) MathF.Floor(amount.Float() * RefundRatio));

        if (TryComp<ContainerManagerComponent>(product, out var containers))
        {
            var prototypes = new List<string>();
            foreach (var container in containers.Containers.Values)
            {
                foreach (var contained in container.ContainedEntities)
                {
                    if (MetaData(contained).EntityPrototype is { } prototype)
                        prototypes.Add(prototype.ID);
                }
            }

            if (prototypes.Count > 0)
                refundable.OriginalContainerPrototypes = prototypes;
        }

        if (HasComp<SolutionManagerComponent>(product) || HasComp<SolutionComponent>(product))
        {
            var reagents = new Dictionary<string, FixedPoint2>();
            foreach (var (_, solution) in _solutionContainer.EnumerateSolutions((product, null)))
            {
                foreach (var (reagent, quantity) in solution.Comp.Solution.Contents)
                {
                    reagents.TryAdd(reagent.Prototype, FixedPoint2.Zero);
                    reagents[reagent.Prototype] += quantity;
                }
            }

            refundable.OriginalSolutionReagents = reagents;
        }
    }

    private void MarkUsed(UplinkRefundableComponent comp)
    {
        if (comp.IsUsed)
            return;

        comp.IsUsed = true;
    }

    [SubscribeLocalEvent]
    private void OnGunShot(Entity<UplinkRefundableComponent> ent, ref GunShotEvent args)
    {
        if (args.Ammo.Count == 0)
            return;

        MarkUsed(ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnMeleeHit(Entity<UplinkRefundableComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || args.HitEntities.Count == 0)
            return;

        MarkUsed(ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnThrown(Entity<UplinkRefundableComponent> ent, ref ThrowEvent args)
    {
        MarkUsed(ent.Comp);
    }

    [SubscribeLocalEvent(after: new[] { typeof(SharedTetherGunSystem) })]
    private void OnAfterInteract(Entity<UplinkRefundableComponent> ent, ref AfterInteractEvent args)
    {
        if (HasComp<GunComponent>(ent) || HasComp<TimerTriggerComponent>(ent))
            return;

        if (TryComp<TetherGunComponent>(ent, out var tether) && tether.Tethered != null)
        {
            MarkUsed(ent.Comp);
            return;
        }

        if (TryComp<ForceGunComponent>(ent, out var force) && force.Tethered != null)
        {
            MarkUsed(ent.Comp);
            return;
        }

        if (args.Handled)
            MarkUsed(ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<UplinkRefundableComponent> ent, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        if (ent.Comp.IsUsed)
            return;

        var user = args.User;
        var target = ent.Owner;

        if (!TryGetUplinkForUser(user, ent.Comp.OriginalUplink, out _, out _))
            return;

        args.Verbs.Add(new InteractionVerb
        {
            Text = Loc.GetString("uplink-refund-verb"),
            Act = () =>
            {
                // A remote uplink's store is in nullspace; give feedback at the user instead.
                if (TryGetUplinkForUser(user, ent.Comp.OriginalUplink, out var currentUplink, out var currentStore))
                    TryRefund(target, ent.Comp, currentUplink, currentStore, user, user);
            },
        });
    }

    [SubscribeLocalEvent]
    private void OnUplinkInteractUsing(Entity<StoreComponent> ent, ref InteractUsingEvent args)
    {
        HandleInteractUsing(ent, ent.Comp, args);
    }

    [SubscribeLocalEvent]
    private void OnRemoteUplinkInteractUsing(Entity<RemoteStoreComponent> ent, ref InteractUsingEvent args)
    {
        if (_store.GetRemoteStore(ent.AsNullable()) is { } store)
            HandleInteractUsing(store, store.Comp, args);
    }

    private void HandleInteractUsing(EntityUid uplink, StoreComponent store, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        var used = args.Used;

        if (HasComp<IdCardComponent>(used))
            return;

        if (!TryComp<UplinkRefundableComponent>(used, out var refundable))
            return;

        if (TryRefund(used, refundable, uplink, store, args.User, args.Target))
            args.Handled = true;
    }

    private bool TryGetUplinkForUser(EntityUid user, EntityUid originalUplink, out EntityUid uplink, out StoreComponent store)
    {
        uplink = default;
        store = default!;

        if (!_mind.TryGetMind(user, out var userMind, out _))
            return false;

        foreach (var held in _hands.EnumerateHeld(user))
        {
            if (TryResolveOriginalStore(held, originalUplink, userMind, out store))
            {
                uplink = originalUplink;
                return true;
            }
        }

        if (!TryComp<ImplantedComponent>(user, out var implanted))
            return false;

        var container = implanted.ImplantContainer;
        if (container == null)
            return false;

        foreach (var implant in container.ContainedEntities)
        {
            if (TryResolveOriginalStore(implant, originalUplink, userMind, out store))
            {
                uplink = originalUplink;
                return true;
            }
        }

        return false;
    }

    private bool TryResolveOriginalStore(EntityUid accessEntity, EntityUid originalUplink, EntityUid userMind,
        out StoreComponent store)
    {
        store = default!;
        if (accessEntity == originalUplink && TryComp<StoreComponent>(accessEntity, out var direct))
            store = direct;
        else if (TryComp<RemoteStoreComponent>(accessEntity, out var access)
                 && _store.GetRemoteStore((accessEntity, access)) is { } remote && remote.Owner == originalUplink)
            store = remote.Comp;
        else
            return false;

        return store.AccountOwner == null || store.AccountOwner == userMind;
    }

    private bool CanRefundItem(EntityUid used, UplinkRefundableComponent refundable, EntityUid user)
    {
        if (refundable.OriginalContainerPrototypes != null)
        {
            var currentPrototypes = new List<string>();
            if (TryComp<ContainerManagerComponent>(used, out var containerManager))
            {
                foreach (var container in containerManager.Containers.Values)
                {
                    foreach (var entity in container.ContainedEntities)
                    {
                        var meta = MetaData(entity);
                        if (meta.EntityPrototype != null)
                            currentPrototypes.Add(meta.EntityPrototype.ID);
                    }
                }
            }

            currentPrototypes.Sort();
            var originalPrototypes = new List<string>(refundable.OriginalContainerPrototypes);
            originalPrototypes.Sort();

            if (!((IEnumerable<string>) currentPrototypes).SequenceEqual(originalPrototypes))
            {
                _popup.PopupEntity(Loc.GetString("uplink-refund-fail-container-changed"), used, user);
                return false;
            }
        }

        if (refundable.OriginalSolutionReagents != null)
        {
            var currentReagents = new Dictionary<string, FixedPoint2>();
            foreach (var (_, soln) in _solutionContainer.EnumerateSolutions((used, null)))
            {
                foreach (var (reagent, quantity) in soln.Comp.Solution.Contents)
                {
                    var id = reagent.Prototype;
                    if (currentReagents.TryGetValue(id, out var existing))
                        currentReagents[id] = existing + quantity;
                    else
                        currentReagents[id] = quantity;
                }
            }

            if (currentReagents.Count != refundable.OriginalSolutionReagents.Count)
            {
                _popup.PopupEntity(Loc.GetString("uplink-refund-fail-solution-changed"), used, user);
                return false;
            }

            foreach (var (reagentId, originalQuantity) in refundable.OriginalSolutionReagents)
            {
                if (!currentReagents.TryGetValue(reagentId, out var currentQuantity))
                {
                    _popup.PopupEntity(Loc.GetString("uplink-refund-fail-solution-changed"), used, user);
                    return false;
                }

                if (currentQuantity != originalQuantity)
                {
                    _popup.PopupEntity(Loc.GetString("uplink-refund-fail-solution-changed"), used, user);
                    return false;
                }
            }
        }

        if (TryComp<BallisticAmmoProviderComponent>(used, out var ballistic))
        {
            var currentCount = ballistic.Count;
            if (currentCount < ballistic.Capacity)
            {
                _popup.PopupEntity(Loc.GetString("uplink-refund-fail-magazine-empty"), used, user);
                return false;
            }
        }

        if (TryComp<CartridgeAmmoComponent>(used, out var cartridge) && cartridge.Spent)
        {
            _popup.PopupEntity(Loc.GetString("uplink-refund-fail-cartridge-spent"), used, user);
            return false;
        }

        return true;
    }

    private bool TryRefund(EntityUid used, UplinkRefundableComponent refundable, EntityUid uplink, StoreComponent store,
        EntityUid user, EntityUid feedbackSource)
    {
        if (HasComp<IdCardComponent>(used) || !HasComp<UplinkRefundStoreComponent>(uplink))
            return false;

        if (refundable.IsUsed)
        {
            _popup.PopupEntity(Loc.GetString("uplink-refund-fail-used"), used, user);
            return false;
        }

        if (!CanRefundItem(used, refundable, user))
            return false;

        if (refundable.OriginalUplink != uplink)
        {
            _popup.PopupEntity(Loc.GetString("uplink-refund-fail-wrong-uplink"), used, user);
            return false;
        }

        if (!_hands.TryGetActiveItem(user, out var activeItem) || activeItem != used)
        {
            _popup.PopupEntity(Loc.GetString("uplink-refund-fail-not-in-hand"), used, user);
            return false;
        }

        if (refundable.RefundAmount <= FixedPoint2.Zero)
        {
            _popup.PopupEntity(Loc.GetString("uplink-refund-fail-zero"), used, user);
            return false;
        }

        var refundAmount = refundable.RefundAmount;
        var currency = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>
        {
            { refundable.Currency, refundAmount }
        };
        if (!_store.TryRefundPurchase(uplink, used, currency, store))
        {
            _popup.PopupEntity(Loc.GetString("uplink-refund-fail-currency"), used, user);
            return false;
        }

        refundable.IsUsed = true;
        QueueDel(used);

        _popup.PopupEntity(
            Loc.GetString("uplink-refund-success", ("amount", refundAmount)),
            feedbackSource,
            user);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Effects/Cargo/ping.ogg"), feedbackSource);

        return true;
    }
}
