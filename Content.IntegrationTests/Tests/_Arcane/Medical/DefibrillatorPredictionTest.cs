using Content.Arcane.Shared.Medical.Heart;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;

namespace Content.IntegrationTests.Tests._Arcane.Medical;

public sealed class DefibrillatorPredictionTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task OnlyTheServerResolvesTheShockOutcome()
    {
        await SpawnTarget("MobHuman");
        var defib = await PlaceInHands("DefibrillatorCompact", enableToggleable: true);
        await Server.WaitPost(() =>
        {
            var heartSystem = SEntMan.System<HeartRateSystem>();
            var heart = heartSystem.GetHeart(STarget!.Value)!.Value;
            heartSystem.SetRate(heart.Owner, heart.Comp!, 0f, true);
            SEntMan.System<MobStateSystem>().ChangeMobState(STarget.Value, MobState.Dead);
        });
        await Pair.RunUntilSynced();

        await Client.WaitAssertion(() =>
        {
            CEntMan.System<SharedDefibrillatorSystem>().Zap(CEntMan.GetEntity(defib), CTarget!.Value, CPlayer);
            Assert.That(CEntMan.GetComponent<MobStateComponent>(CTarget.Value).CurrentState, Is.EqualTo(MobState.Dead),
                "The client must wait for the server's physiology and treatment result.");
        });
        await Server.WaitAssertion(() =>
        {
            SEntMan.System<SharedDefibrillatorSystem>().Zap(SEntMan.GetEntity(defib), STarget!.Value, SPlayer);
            Assert.That(SEntMan.GetComponent<MobStateComponent>(STarget.Value).CurrentState, Is.Not.EqualTo(MobState.Dead));
        });
    }
}
