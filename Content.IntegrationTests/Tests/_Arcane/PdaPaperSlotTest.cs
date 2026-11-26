// Arcane-Start
using Content.Client.PDA;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.PDA;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class PdaPaperSlotTest : InteractionTest
{
    [TestCase("PassengerPDA")]
    [TestCase("MimePDA")]
    public async Task PaperEjectsThroughPdaAndCartridgesStillFit(string prototype)
    {
        await SpawnTarget(prototype);
        await InteractUsing("PaperOffice");
        Assert.That(HandSys.GetActiveItem((SEntMan.GetEntity(Player), Hands)), Is.Null);
        await Server.WaitAssertion(() =>
        {
            var slot = SEntMan.GetComponent<PdaComponent>(STarget!.Value).PaperSlot!;
            Assert.That(slot.HasItem, Is.True);
            if (prototype == "MimePDA")
            {
                Assert.That(slot.InsertSound, Is.Null);
                Assert.That(slot.EjectSound, Is.Null);
            }
        });

        await Activate();
        await ClickControl<PdaMenu>(nameof(PdaMenu.EjectPaperButton));
        await RunTicks(5);
        var held = HandSys.GetActiveItem((SEntMan.GetEntity(Player), Hands));
        Assert.That(held, Is.Not.Null);
        AssertPrototype("PaperOffice", SEntMan.GetNetEntity(held));

        await Drop();
        await InteractUsing("NewsReaderCartridge");
        Assert.That(HandSys.GetActiveItem((SEntMan.GetEntity(Player), Hands)), Is.Null);
        await Server.WaitAssertion(() => Assert.That(SEntMan.GetComponent<PdaComponent>(STarget!.Value).PaperSlot!.HasItem, Is.False));
    }
}
// Arcane-End
