// Arcane-Start
using System.Linq;
using Content.Server.Store.Systems;
using Content.Server.Traitor.Uplink;
using Content.Shared.FixedPoint;
using Content.Shared.Store;
using Content.Shared.Store.Components;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class UplinkBalanceTest : GameTest
{
    private static readonly (ProtoId<ListingPrototype> Listing, int Price)[] ApprovedPrices =
    [
        ("UplinkBulldog", 80),
        ("UplinkEnergyCrossbow", 45),
        ("UplinkGrenadeFrag", 9),
        ("UplinkGrenadeBlast", 9),
        ("UplinkCombatMedkitPills", 15),
        ("UplinkKravMagaImplanter", 35),
        ("UplinkPistolMagazineCaselessTranquilizer", 17),
        ("UplinkPistolMagazineCaselessPoison", 15),
        ("UplinkCombatStandardMedkit", 30),
        ("UplinkCombatAdvancedMedkit", 60),
        ("UplinkScramImplanter", 20),
    ];

    private static readonly ProtoId<ListingPrototype> Scram = "UplinkScramImplanter";
    private static readonly EntProtoId OrdinaryUplink = "BaseUplinkRadio";
    private static readonly EntProtoId NukeOpsUplink = "BaseUplinkRadio225TC";
    private static readonly EntProtoId Buyer = "MobHuman";

    [Test]
    public async Task ApprovedTelecrystalPrices()
    {
        await Server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var (listingId, price) in ApprovedPrices)
                {
                    var listing = Server.ProtoMan.Index(listingId);
                    Assert.That(listing.Cost[UplinkSystem.TelecrystalCurrencyPrototype],
                        Is.EqualTo((FixedPoint2) price), listingId.Id);
                }
            });
        });
    }

    [Test]
    public async Task ScramAvailableOnlyOnOrdinaryUplink()
    {
        var map = await Pair.CreateTestMap();

        await Server.WaitAssertion(() =>
        {
            var buyer = SSpawnAtPosition(Buyer.Id, map.GridCoords);
            var ordinary = SSpawnAtPosition(OrdinaryUplink.Id, map.GridCoords);
            var nukeOps = SSpawnAtPosition(NukeOpsUplink.Id, map.GridCoords);
            var storeSystem = Server.System<StoreSystem>();
            var ordinaryStore = SComp<StoreComponent>(ordinary);
            var nukeOpsStore = SComp<StoreComponent>(nukeOps);
            var listings = new[] { new ListingDataWithCostModifiers(Server.ProtoMan.Index(Scram)) };

            Assert.Multiple(() =>
            {
                Assert.That(storeSystem.GetAvailableListings(buyer, listings, ordinaryStore.Categories, ordinary)
                    .Select(listing => listing.ID), Does.Contain(Scram.Id));
                Assert.That(storeSystem.GetAvailableListings(buyer, listings, nukeOpsStore.Categories, nukeOps)
                    .Select(listing => listing.ID), Does.Not.Contain(Scram.Id));
            });
        });
    }
}
// Arcane-End
