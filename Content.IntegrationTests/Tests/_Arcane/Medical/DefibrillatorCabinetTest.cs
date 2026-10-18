using System.Linq;
using Content.Client.Power;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Cabinet;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Power.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.PowerCell;
using Robust.Client.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane.Medical;

public sealed class DefibrillatorCabinetTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task CabinetChargingAndLightFollowItsPowerReceiver()
    {
        await SpawnTarget("DefibrillatorCabinetFilled");
        Entity<BatteryComponent> battery = default;
        await Server.WaitPost(() =>
        {
            var slot = SEntMan.GetComponent<ItemSlotsComponent>(STarget!.Value).Slots["ItemCabinet"];
            Assert.That(SEntMan.System<PowerCellSystem>().TryGetBatteryFromEntityOrSlot(slot.Item!.Value, out var cell), Is.True);
            battery = cell!.Value;
            SEntMan.System<SharedBatterySystem>().SetCharge(battery.AsNullable(), 0f);
            // Supply the receiver directly; network distribution is covered by the power tests.
            SEntMan.System<SharedPowerReceiverSystem>().SetNeedsPower(STarget.Value, false);
        });
        await RunSeconds(1f);
        await AssertPowerLight(true);
        await Server.WaitAssertion(() =>
            Assert.That(SEntMan.System<SharedBatterySystem>().GetCharge(battery.AsNullable()), Is.GreaterThan(0f)));

        await Server.WaitPost(() =>
            SEntMan.System<SharedPowerReceiverSystem>().SetNeedsPower(STarget!.Value, true));
        await Pair.RunUntilSynced();
        float unpoweredCharge = 0f;
        await Server.WaitPost(() =>
            unpoweredCharge = SEntMan.System<SharedBatterySystem>().GetCharge(battery.AsNullable()));
        await RunSeconds(1f);
        await AssertPowerLight(false);
        await Server.WaitAssertion(() =>
            Assert.That(SEntMan.System<SharedBatterySystem>().GetCharge(battery.AsNullable()), Is.EqualTo(unpoweredCharge)));
    }

    private async Task AssertPowerLight(bool powered)
    {
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var uid = CTarget!.Value;
            var sprite = CEntMan.GetComponent<SpriteComponent>(uid);
            var layer = CEntMan.System<SpriteSystem>().LayerMapGet(uid, PowerDeviceVisualLayers.Powered);
            Assert.That(sprite[layer].Visible, Is.EqualTo(powered));
        });
    }

    [Test]
    public async Task CabinetReplicatesItsDoorAndRepeatedTwoHandedTransfers()
    {
        await SpawnTarget("DefibrillatorCabinetFilledOpen");
        await Pair.RunUntilSynced();
        await AssertCabinet("open", true);

        for (var i = 0; i < 2; i++)
        {
            // Pick up through the server so the client must adopt the replicated virtual hand item.
            EntityUid defib = default;
            await Server.WaitPost(() =>
            {
                Assert.That(SEntMan.System<ItemSlotsSystem>().TryEject(
                    STarget!.Value, "ItemCabinet", SPlayer, out var item, doAfter: false), Is.True);
                defib = item!.Value;
                Assert.That(HandSys.TryPickupAnyHand(SPlayer, defib), Is.True);
            });
            await AssertCabinet("open", false);
            await Client.WaitAssertion(() =>
            {
                var held = CEntMan.System<SharedHandsSystem>().EnumerateHeld(CPlayer).ToArray();
                Assert.That(held, Has.Length.EqualTo(2));
                var virtualItem = held.Single(CEntMan.HasComponent<VirtualItemComponent>);
                Assert.That(CEntMan.GetNetEntity(virtualItem).IsClientSide(), Is.False,
                    "Applying a server pickup must not create another client-only virtual item.");
            });

            await Server.WaitPost(() =>
                Assert.That(SEntMan.System<ItemSlotsSystem>().TryInsert(
                    STarget!.Value, "ItemCabinet", defib, SPlayer), Is.True));
            await AssertCabinet("open", true);
            await Client.WaitAssertion(() =>
                Assert.That(CEntMan.System<SharedHandsSystem>().EnumerateHeld(CPlayer), Is.Empty));

            await Server.WaitPost(() => SEntMan.System<OpenableSystem>().SetOpen(STarget!.Value, false));
            await AssertCabinet("closed", true);
            await Server.WaitPost(() => SEntMan.System<OpenableSystem>().SetOpen(STarget!.Value));
            await AssertCabinet("open", true);
        }
    }

    private async Task AssertCabinet(string door, bool occupied)
    {
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            var uid = CTarget!.Value;
            var sprite = CEntMan.GetComponent<SpriteComponent>(uid);
            var sprites = CEntMan.System<SpriteSystem>();
            Assert.That(sprite[sprites.LayerMapGet(uid, OpenableVisuals.Layer)].RsiState.ToString(), Is.EqualTo(door));
            Assert.That(sprite[sprites.LayerMapGet(uid, ItemCabinetVisuals.Layer)].Visible, Is.EqualTo(occupied));
        });
    }
}
