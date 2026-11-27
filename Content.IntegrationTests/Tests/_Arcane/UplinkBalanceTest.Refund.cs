// Arcane-Start
using System.Collections.Generic;
using System.Linq;
using Content.Arcane.Server.Store;
using Content.IntegrationTests.Fixtures;
using Content.Server.PDA.Ringer;
using Content.Server.Store.Systems;
using Content.Server.Traitor.Uplink;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Mind;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Content.Shared.StoreDiscount.Components;
using Content.Shared.Throwing;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed partial class UplinkBalanceTest
{
    private const string RefundListing = "ArcaneTestUplinkRefund";
    private const string RefundCardListing = "ArcaneTestUplinkRefundCard";
    private const string RefundContainerListing = "ArcaneTestUplinkRefundContainer";
    private const string RefundSolutionListing = "ArcaneTestUplinkRefundSolution";
    private const string RefundWizardListing = "ArcaneTestWizardRefund";
    private const string RefundBriefcaseListing = "ArcaneTestRefundBriefcase";
    private const string RefundStampListing = "ArcaneTestRefundStamp";

    [TestPrototypes]
    private const string RefundPrototypes = """
- type: listing
  id: ArcaneTestUplinkRefund
  discountCategory: null
  productEntity: Crowbar
  cost:
    Telecrystal: 9
  categories: [ UplinkPointless ]
- type: listing
  id: ArcaneTestUplinkRefundCard
  discountCategory: null
  productEntity: AgentIDCard
  cost:
    Telecrystal: 9
  categories: [ UplinkPointless ]
- type: listing
  id: ArcaneTestUplinkRefundContainer
  discountCategory: null
  productEntity: BoxSurvival
  cost:
    Telecrystal: 9
  categories: [ UplinkPointless ]
- type: listing
  id: ArcaneTestUplinkRefundSolution
  discountCategory: null
  productEntity: DrinkWaterBottleFull
  cost:
    Telecrystal: 9
  categories: [ UplinkPointless ]
- type: listing
  id: ArcaneTestWizardRefund
  discountCategory: null
  productEntity: Crowbar
  cost:
    WizCoin: 9
  categories: [ SpellbookEquipment ]
- type: listing
  id: ArcaneTestRefundBriefcase
  discountCategory: null
  productEntity: BriefcaseWeaponC20Filled
  cost:
    Telecrystal: 9
  categories: [ UplinkPointless ]
- type: listing
  id: ArcaneTestRefundStamp
  discountCategory: null
  productEntity: RubberStampChameleon
  cost:
    Telecrystal: 9
  categories: [ UplinkPointless ]
""";

    [Test]
    public async Task UntouchedBriefcaseRemainsRefundable()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var buyer = SSpawnAtPosition(Buyer.Id, map.GridCoords);
            var uplink = SSpawnAtPosition(OrdinaryUplink.Id, map.GridCoords);
            SComp<StoreComponent>(uplink).Balance[UplinkSystem.TelecrystalCurrencyPrototype] = 100;
            var product = BuyRefundItem(uplink, buyer, RefundBriefcaseListing);
            Assert.That(SComp<UplinkRefundableComponent>(product).OriginalContainedItems.Count, Is.GreaterThan(0));
            Assert.That(ReturnRefundItem(uplink, buyer, product), Is.True);
        });
    }

    [Test]
    public async Task ReplacingNestedMagazineDoesNotMatchOriginalContents()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var buyer = SSpawnAtPosition(Buyer.Id, map.GridCoords);
            var uplink = SSpawnAtPosition(OrdinaryUplink.Id, map.GridCoords);
            SComp<StoreComponent>(uplink).Balance[UplinkSystem.TelecrystalCurrencyPrototype] = 100;
            var product = BuyRefundItem(uplink, buyer, RefundBriefcaseListing);
            var gun = SComp<StorageComponent>(product).Container.ContainedEntities.Single(SEntMan.HasComponent<GunComponent>);
            var slots = Server.System<ItemSlotsSystem>();
            var magazine = slots.GetItemOrNull(gun, "gun_magazine");
            Assert.That(magazine, Is.Not.Null);
            SEntMan.DeleteEntity(magazine!.Value);
            var replacement = SSpawnAtPosition("MagazinePistolSubMachineGun", map.GridCoords);
            Assert.That(slots.TryInsert(gun, "gun_magazine", replacement, null), Is.True);
            Assert.That(SComp<UplinkRefundableComponent>(product).IsUsed, Is.False);
            Assert.That(ReturnRefundItem(uplink, buyer, product), Is.False);
        });
    }

    [Test]
    public async Task SuccessfulStampMakesPurchaseNonRefundable()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var buyer = SSpawnAtPosition(Buyer.Id, map.GridCoords);
            var uplink = SSpawnAtPosition(OrdinaryUplink.Id, map.GridCoords);
            SComp<StoreComponent>(uplink).Balance[UplinkSystem.TelecrystalCurrencyPrototype] = 100;
            var stamp = BuyRefundItem(uplink, buyer, RefundStampListing);
            var paper = SSpawnAtPosition("Paper", map.GridCoords);
            SEntMan.EventBus.RaiseLocalEvent(paper, new InteractUsingEvent(buyer, stamp, paper, map.GridCoords));
            Assert.That(SComp<PaperComponent>(paper).StampedBy, Is.Not.Empty);
            Assert.That(SComp<UplinkRefundableComponent>(stamp).IsUsed, Is.True);
            Assert.That(ReturnRefundItem(uplink, buyer, stamp), Is.False);
        });
    }

    [Test]
    public async Task FiringExtractedGunMakesBriefcaseNonRefundable()
    {
        var map = await Pair.CreateTestMap();
        EntityUid buyer = default;
        EntityUid uplink = default;
        EntityUid product = default;
        EntityUid gun = default;
        await Server.WaitAssertion(() =>
        {
            buyer = SSpawnAtPosition(Buyer.Id, map.GridCoords);
            uplink = SSpawnAtPosition(OrdinaryUplink.Id, map.GridCoords);
            SComp<StoreComponent>(uplink).Balance[UplinkSystem.TelecrystalCurrencyPrototype] = 100;
            product = BuyRefundItem(uplink, buyer, RefundBriefcaseListing);
            var storage = SComp<StorageComponent>(product);
            gun = storage.Container.ContainedEntities.Single(SEntMan.HasComponent<GunComponent>);
            Assert.That(Server.System<SharedContainerSystem>().Remove(gun, storage.Container, destination: map.GridCoords), Is.True);
        });
        await Pair.RunUntilSynced();
        await Server.WaitAssertion(() =>
        {
            Assert.That(Server.System<SharedHandsSystem>().TryPickupAnyHand(buyer, gun, checkActionBlocker: false), Is.True);
            Server.System<SharedGunSystem>().UseChambered(gun, SComp<ChamberMagazineAmmoProviderComponent>(gun), buyer);
        });
        await Pair.RunTicksSync(10);
        await Server.WaitAssertion(() =>
        {
            var guns = Server.System<SharedGunSystem>();
            Assert.That(guns.AttemptShoot(buyer, (gun, SComp<GunComponent>(gun)), map.GridCoords.Offset(new Vector2(2, 0))), Is.True);
            Assert.That(SComp<UplinkRefundableComponent>(product).IsUsed, Is.True);
            Assert.That(Server.System<SharedHandsSystem>().TryDrop(buyer, gun), Is.True);
            Assert.That(Server.System<SharedStorageSystem>().Insert(product, gun, out _, user: buyer), Is.True);
            Assert.That(ReturnRefundItem(uplink, buyer, product), Is.False);
        });
    }

    [TestCase("BaseUplinkRadio", false)]
    [TestCase("BaseUplinkRadio225TC", true)]
    [TestCase("BaseUplinkRadio225TCMedic", true)]
    public async Task UnusedUplinkPurchaseRefundsOnce(string uplinkPrototype, bool leaveStartingMap)
    {
        var map = await Pair.CreateTestMap();
        var otherMap = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var buyer = SSpawnAtPosition(Buyer.Id, map.GridCoords);
            var uplink = SSpawnAtPosition(uplinkPrototype, map.GridCoords);
            var store = SComp<StoreComponent>(uplink);
            store.Balance[UplinkSystem.TelecrystalCurrencyPrototype] = 100;
            Assert.That(SHasComp<UplinkRefundStoreComponent>(uplink), Is.True);
            Assert.That(store.RefundAllowed, Is.False, "The 80% path must not depend on the full-refund flag.");
            if (leaveStartingMap)
            {
                SEntMan.System<SharedTransformSystem>().SetCoordinates(uplink, otherMap.GridCoords);
                SEntMan.System<SharedTransformSystem>().SetCoordinates(buyer, otherMap.GridCoords);
            }

            var product = BuyRefundItem(uplink, buyer);
            Assert.That(store.Balance[UplinkSystem.TelecrystalCurrencyPrototype], Is.EqualTo((FixedPoint2) 91));
            Assert.That(ReturnRefundItem(uplink, buyer, product), Is.True);
            Assert.That(store.Balance[UplinkSystem.TelecrystalCurrencyPrototype], Is.EqualTo((FixedPoint2) 98));
            Assert.That(ReturnRefundItem(uplink, buyer, product), Is.False);
            Assert.That(Server.System<StoreSystem>().RefundListing(uplink, store, product, buyer, false), Is.False);
            Assert.That(store.Balance[UplinkSystem.TelecrystalCurrencyPrototype], Is.EqualTo((FixedPoint2) 98));
            Assert.That(store.BoughtEntities, Does.Not.Contain(product));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RemoteUplinkRefundWorksThroughPdaAndImplant(bool implant)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var buyer = SSpawnAtPosition(Buyer.Id, map.GridCoords);
            var minds = Server.System<SharedMindSystem>();
            var mind = minds.CreateMind(null);
            minds.TransferTo(mind, buyer, mind: mind);
            var uplinks = Server.System<UplinkSystem>();
            var stores = Server.System<StoreSystem>();
            EntityUid access;
            EntityUid storeUid;
            if (implant)
            {
                Assert.That(uplinks.AddUplink(buyer, 100, out _), Is.EqualTo(AddUplinkResult.Implant));
                access = SComp<ImplantedComponent>(buyer).ImplantContainer!.ContainedEntities
                    .Single(SEntMan.HasComponent<RemoteStoreComponent>);
                storeUid = stores.GetRemoteStore((access, null))!.Value.Owner;
            }
            else
            {
                access = SSpawnAtPosition("PassengerPDA", map.GridCoords);
                Assert.That(uplinks.AddUplink(buyer, 100, out var code, access), Is.EqualTo(AddUplinkResult.Pda));
                Assert.That(Server.System<RingerSystem>().TryMatchRingtoneToStore(code!, out var linked), Is.True);
                storeUid = linked!.Value;
                stores.SetRemoteStore((access, null), storeUid);
            }

            var store = SComp<StoreComponent>(storeUid);
            var before = store.Balance[UplinkSystem.TelecrystalCurrencyPrototype];
            Assert.That(SHasComp<UplinkRefundStoreComponent>(storeUid), Is.True);
            var product = BuyRefundItem(access, buyer);
            if (implant)
            {
                var verbs = new GetVerbsEvent<InteractionVerb>(buyer, product, product,
                    SComp<HandsComponent>(buyer), true, true, true, []);
                SEntMan.EventBus.RaiseLocalEvent(product, verbs);
                var refund = verbs.Verbs.Single(verb => verb.Text == Loc.GetString("uplink-refund-verb"));
                refund.Act!();
            }
            else
            {
                Assert.That(ReturnRefundItem(access, buyer, product), Is.True);
            }
            Assert.That(store.Balance[UplinkSystem.TelecrystalCurrencyPrototype], Is.EqualTo(before - 2));
            // These entities are spawned by production systems rather than the fixture's tracked spawn helpers.
            minds.WipeMind(mind);
            // Delete the body before its LastMind reference points at a deleted entity.
            SEntMan.DeleteEntity(buyer);
            SEntMan.DeleteEntity(mind);
            SEntMan.DeleteEntity(storeUid);
        });
    }

    [TestCase(-4, 4, true)]
    [TestCase(-8, 0, false)]
    public async Task UplinkRefundUsesActualDiscountedCost(int discount, int refunded, bool succeeds)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var buyer = SSpawnAtPosition(Buyer.Id, map.GridCoords);
            var uplink = SSpawnAtPosition(OrdinaryUplink.Id, map.GridCoords);
            var store = SComp<StoreComponent>(uplink);
            store.Balance[UplinkSystem.TelecrystalCurrencyPrototype] = 100;
            var listing = store.FullListingsCatalog.Single(item => item.ID == RefundListing);
            var category = Server.ProtoMan.EnumeratePrototypes<DiscountCategoryPrototype>().First().ID;
            var amount = new Dictionary<ProtoId<CurrencyPrototype>, FixedPoint2>
            {
                [UplinkSystem.TelecrystalCurrencyPrototype] = discount,
            };
            listing.AddCostModifier(category, amount);
            SEntMan.EnsureComponent<StoreDiscountComponent>(uplink).Discounts =
            [
                new StoreDiscountData
                {
                    ListingId = RefundListing,
                    DiscountCategory = category,
                    DiscountAmountByCurrency = amount,
                    Count = 1,
                },
            ];

            var product = BuyRefundItem(uplink, buyer);
            Assert.That(listing.Cost[UplinkSystem.TelecrystalCurrencyPrototype], Is.EqualTo((FixedPoint2) 9),
                "The production discount observer must reset the listing price after purchase.");
            Assert.That(ReturnRefundItem(uplink, buyer, product), Is.EqualTo(succeeds));
            Assert.That(store.Balance[UplinkSystem.TelecrystalCurrencyPrototype],
                Is.EqualTo((FixedPoint2) (100 - 9 - discount + refunded)));
        });
    }

    [TestCase("used")]
    [TestCase("wrong uplink")]
    [TestCase("not held")]
    [TestCase("ID card")]
    [TestCase("contents changed")]
    [TestCase("solution changed")]
    public async Task IneligibleUplinkPurchaseCannotBeRefunded(string reason)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var buyer = SSpawnAtPosition(Buyer.Id, map.GridCoords);
            var uplink = SSpawnAtPosition(OrdinaryUplink.Id, map.GridCoords);
            var store = SComp<StoreComponent>(uplink);
            store.Balance[UplinkSystem.TelecrystalCurrencyPrototype] = 100;
            var listing = reason switch
            {
                "ID card" => RefundCardListing,
                "contents changed" => RefundContainerListing,
                "solution changed" => RefundSolutionListing,
                _ => RefundListing,
            };
            var product = BuyRefundItem(uplink, buyer, listing);
            switch (reason)
            {
                case "used":
                    var thrown = new ThrowEvent(buyer, product);
                    SEntMan.EventBus.RaiseLocalEvent(product, ref thrown);
                    break;
                case "wrong uplink":
                    uplink = SSpawnAtPosition(OrdinaryUplink.Id, map.GridCoords);
                    break;
                case "not held":
                    DropRefundItem(buyer, product);
                    break;
                case "contents changed":
                    var contained = SComp<ContainerManagerComponent>(product).Containers.Values
                        .SelectMany(container => container.ContainedEntities).First();
                    SEntMan.DeleteEntity(contained);
                    break;
                case "solution changed":
                    var solutions = Server.System<SharedSolutionContainerSystem>();
                    var solution = solutions.EnumerateSolutions((product, null)).First().Solution;
                    Assert.That(solutions.RemoveReagent(solution, "Water", 1), Is.EqualTo((FixedPoint2) 1));
                    break;
            }
            var refunded = ReturnRefundItem(uplink, buyer, product);
            if (reason == "not held")
                TestContext.Out.WriteLine($"not-held result: handled={refunded}, "
                    + $"balance={store.Balance[UplinkSystem.TelecrystalCurrencyPrototype]}, deleted={SEntMan.Deleted(product)}");
            Assert.That(refunded, Is.False);
            Assert.That(store.Balance[UplinkSystem.TelecrystalCurrencyPrototype], Is.EqualTo((FixedPoint2) 91));
            Assert.That(SEntMan.Deleted(product), Is.False);
        });
    }

    [Test]
    public async Task FullRefundConsumesSameReceiptAndWizardKeepsFullRefund()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var buyer = SSpawnAtPosition(Buyer.Id, map.GridCoords);
            var uplink = SSpawnAtPosition(OrdinaryUplink.Id, map.GridCoords);
            var store = SComp<StoreComponent>(uplink);
            store.Balance[UplinkSystem.TelecrystalCurrencyPrototype] = 100;
            var product = BuyRefundItem(uplink, buyer);
            Assert.That(Server.System<StoreSystem>().RefundListing(uplink, store, product, buyer, false), Is.True);
            Assert.That(ReturnRefundItem(uplink, buyer, product), Is.False);
            Assert.That(store.Balance[UplinkSystem.TelecrystalCurrencyPrototype], Is.EqualTo((FixedPoint2) 100));

            var grimoire = SSpawnAtPosition("WizardsGrimoire", map.GridCoords);
            var wizardStore = SComp<StoreComponent>(grimoire);
            Assert.That(SHasComp<UplinkRefundStoreComponent>(grimoire), Is.False);
            var wizardProduct = BuyRefundItem(grimoire, buyer, RefundWizardListing);
            Assert.That(SHasComp<UplinkRefundableComponent>(wizardProduct), Is.False);
            Assert.That(Server.System<StoreSystem>().RefundListing(grimoire, wizardStore, wizardProduct, buyer, false), Is.True);
            Assert.That(wizardStore.Balance["WizCoin"], Is.EqualTo((FixedPoint2) 10));
        });
    }

    private EntityUid BuyRefundItem(EntityUid access, EntityUid buyer, string listing = RefundListing)
    {
        SEntMan.EventBus.RaiseLocalEvent(access, new StoreBuyListingMessage(listing, null) { Actor = buyer });
        Assert.That(Server.System<SharedHandsSystem>().TryGetActiveItem(buyer, out var product), Is.True);
        return product!.Value;
    }

    private void DropRefundItem(EntityUid buyer, EntityUid product)
    {
        try
        {
            var hands = Server.System<SharedHandsSystem>();
            var beforeMap = SComp<TransformComponent>(product).MapUid;
            var dropped = hands.TryDrop(buyer, product);
            var stillHeld = hands.IsHolding(buyer, product);
            STryComp<TransformComponent>(product, out var afterTransform);
            TestContext.Out.WriteLine($"not-held setup: seed={Pair.ServerSeed}, product={SPrototype(product)?.ID}, "
                + $"buyerMap={SComp<TransformComponent>(buyer).MapUid}, beforeMap={beforeMap}, "
                + $"afterMap={afterTransform?.MapUid}, dropped={dropped}, held={stillHeld}");
            Assert.That(dropped, Is.True);
            Assert.That(stillHeld, Is.False, "The rejected refund must use an item actually removed from all hands.");
        }
        catch (Exception exception)
        {
            // Pair.DisposeAsync can otherwise replace the primary failure with a dirty-disposed warning.
            TestContext.Out.WriteLine($"not-held primary failure: {exception}");
            throw;
        }
    }

    private bool ReturnRefundItem(EntityUid access, EntityUid buyer, EntityUid product)
    {
        var coordinates = SComp<TransformComponent>(buyer).Coordinates;
        var interaction = new InteractUsingEvent(buyer, product, access, coordinates);
        SEntMan.EventBus.RaiseLocalEvent(access, interaction);
        return interaction.Handled;
    }
}
// Arcane-End
