using Content.Client.Drowsiness;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Bed.Sleep;
using Content.Shared.Drowsiness;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane.Medical;

[TestOf(typeof(DrowsinessStatusEffectComponent))]
public sealed class OpioidEffectTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly StatusEffectsSystem _effects = null!;
    [SidedDependency(Side.Client)] private readonly IOverlayManager _overlays = null!;

    [Test]
    public async Task VisualEffectDoesNotCauseSleepingButDrowsinessStillDoes()
    {
        await Pair.CreateTestMap();
        EntityUid patient = default;
        await Server.WaitPost(() =>
        {
            patient = SSpawnAtPosition("MobHuman", TestMap!.GridCoords);
            Assert.That(_effects.TryAddStatusEffectDuration(patient, "StatusEffectOpiated",
                out var effect, TimeSpan.FromSeconds(60)), Is.True);
            SComp<DrowsinessStatusEffectComponent>(effect!.Value).NextIncidentTime = SGameTiming.CurTime;
        });

        await RunSeconds(0.2f);
        await Server.WaitPost(() =>
        {
            Assert.That(_effects.TryGetStatusEffect(patient, SleepingSystem.StatusEffectForcedSleeping, out _), Is.False);
            Assert.That(_effects.TryAddStatusEffectDuration(patient, "StatusEffectDrowsiness",
                out var effect, TimeSpan.FromSeconds(60)), Is.True);
            SComp<DrowsinessStatusEffectComponent>(effect!.Value).NextIncidentTime = SGameTiming.CurTime;
        });

        await RunSeconds(0.2f);
        await Server.WaitPost(() =>
            Assert.That(_effects.TryGetStatusEffect(patient, SleepingSystem.StatusEffectForcedSleeping, out _), Is.True));
    }

    [Test]
    public async Task CombinedEffectsRecoverAndFollowTheControlledEntity()
    {
        await Pair.CreateTestMap();
        EntityUid patient = default;
        EntityUid healthy = default;
        await Server.WaitPost(() =>
        {
            patient = SSpawnAtPosition("MobHuman", TestMap!.GridCoords);
            healthy = SSpawnAtPosition("MobHuman", TestMap.GridCoords);
            Assert.That(_effects.TryAddStatusEffectDuration(patient, "StatusEffectOpiated", TimeSpan.FromSeconds(60)), Is.True);
            Assert.That(_effects.TryAddStatusEffectDuration(patient, "StatusEffectDrowsiness", TimeSpan.FromSeconds(60)), Is.True);
            Server.PlayerMan.SetAttachedEntity(ServerSession, patient);
        });

        try
        {
            await AssertOverlayAfterUpdate(true);
            await Server.WaitPost(() => Assert.That(_effects.TryRemoveStatusEffect(patient, "StatusEffectDrowsiness"), Is.True));
            await AssertOverlayAfterUpdate(true);

            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, healthy));
            await AssertOverlayAfterUpdate(false);
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, patient));
            await AssertOverlayAfterUpdate(true);

            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, null));
            await AssertOverlayAfterUpdate(false);
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, patient));
            await AssertOverlayAfterUpdate(true);

            await Server.WaitPost(() => Assert.That(_effects.TryRemoveStatusEffect(patient, "StatusEffectOpiated"), Is.True));
            await AssertOverlayAfterUpdate(false);

            await Server.WaitPost(() =>
                Assert.That(_effects.TryAddStatusEffectDuration(patient, "StatusEffectOpiated", TimeSpan.FromSeconds(0.5)), Is.True));
            await AssertOverlayAfterUpdate(true);
            await RunSeconds(0.5f);
            await AssertOverlayAfterUpdate(false);
        }
        finally
        {
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, null));
            await AssertOverlayAfterUpdate(false);
        }
    }

    private async Task AssertOverlayAfterUpdate(bool present)
    {
        await RunSeconds(0.2f);
        await Client.WaitPost(() =>
            Assert.That(_overlays.HasOverlay<DrowsinessOverlay>(), Is.EqualTo(present)));
    }
}
