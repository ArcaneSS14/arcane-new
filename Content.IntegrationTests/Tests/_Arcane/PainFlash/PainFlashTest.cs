using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.PainFlash;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Timing;
using ClientPainFlashSystem = Content.Arcane.Client.PainFlash.PainFlashSystem;
using PainFlashOverlay = Content.Arcane.Client.PainFlash.PainFlashOverlay;

namespace Content.IntegrationTests.Tests._Arcane.PainFlash;

[TestOf(typeof(ClientPainFlashSystem))]
public sealed class PainFlashTest : GameTest
{
    private static readonly FixedPoint2 ExpectedPain = 20;

    [SidedDependency(Side.Server)] private readonly PainFlashTestSystem _serverMessages = null!;
    [SidedDependency(Side.Client)] private readonly ClientPainFlashSystem _painFlash = null!;
    [SidedDependency(Side.Client)] private readonly IOverlayManager _overlays = null!;
    [SidedDependency(Side.Client)] private readonly IPlayerManager _player = null!;

    [Test]
    public async Task VoxInheritsPainFlashWithoutPerishable()
    {
        await Server.WaitAssertion(() =>
        {
            var human = SSpawn("MobHuman");
            var vox = SSpawn("MobVox");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(SHasComp<PainFlashComponent>(human), Is.True);
                Assert.That(SHasComp<PerishableComponent>(human), Is.True);
                Assert.That(SHasComp<PainFlashComponent>(vox), Is.True);
                Assert.That(SHasComp<PerishableComponent>(vox), Is.False);
            }
        });
    }

    [Test]
    public async Task MatchingPredictionAndConfirmationProducesSingleFlash()
    {
        await ConfigurePainFlash();
        var (body, _, bodyNet, _) = await SpawnBodies();

        try
        {
            await Pair.SyncTicks(0);
            GameTick predictionTick = default;
            float predictedPain = default;

            await Client.WaitAssertion(() =>
            {
                var clientBody = CEntMan.GetEntity(bodyNet);
                predictionTick = CGameTiming.CurTick;
                RaiseDamage(CEntMan, clientBody);
                predictedPain = GetOverlay().CurrentPain;
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(predictedPain, Is.GreaterThan(0f));
                    Assert.That(_painFlash.PendingPredictionCount, Is.EqualTo(1));
                }
            });

            await Server.WaitAssertion(() =>
            {
                Assert.That(SGameTiming.CurTick, Is.EqualTo(predictionTick));
                RaiseDamage(SEntMan, body);
            });

            await Pair.ReallyBeIdle(5);
            await Client.WaitAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(_painFlash.PendingPredictionCount, Is.Zero);
                    Assert.That(GetOverlay().CurrentPain, Is.GreaterThan(0f).And.LessThanOrEqualTo(predictedPain));
                }
            });
        }
        finally
        {
            await DetachPlayer();
        }
    }

    [Test]
    public async Task DelayedOldBodyConfirmationDoesNotAffectOrConsumeCurrentPrediction()
    {
        await ConfigurePainFlash();
        var (_, currentBody, oldBodyNet, currentBodyNet) = await SpawnBodies();

        try
        {
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession!, currentBody));
            await RunTicksSync(5);
            await Client.WaitAssertion(() =>
                Assert.That(CEntMan.GetNetEntity(_player.LocalEntity), Is.EqualTo(currentBodyNet)));

            await Pair.SyncTicks(0);
            GameTick predictionTick = default;
            float predictedPain = default;

            await Client.WaitAssertion(() =>
            {
                var clientBody = CEntMan.GetEntity(currentBodyNet);
                predictionTick = CGameTiming.CurTick;
                RaiseDamage(CEntMan, clientBody);
                predictedPain = GetOverlay().CurrentPain;
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(predictedPain, Is.GreaterThan(0f));
                    Assert.That(_painFlash.PendingPredictionCount, Is.EqualTo(1));
                }
            });

            await Server.WaitPost(() => _serverMessages.Send(
                new PainFlashMessage(oldBodyNet, ExpectedPain, predictionTick),
                ServerSession!));
            await Pair.ReallyBeIdle(5);

            float painAfterOldConfirmation = default;
            await Client.WaitAssertion(() =>
            {
                painAfterOldConfirmation = GetOverlay().CurrentPain;
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(_painFlash.PendingPredictionCount, Is.EqualTo(1));
                    Assert.That(painAfterOldConfirmation, Is.GreaterThan(0f).And.LessThanOrEqualTo(predictedPain));
                }
            });

            await Server.WaitPost(() => _serverMessages.Send(
                new PainFlashMessage(currentBodyNet, ExpectedPain, predictionTick),
                ServerSession!));
            await Pair.ReallyBeIdle(5);

            await Client.WaitAssertion(() =>
            {
                using (Assert.EnterMultipleScope())
                {
                    Assert.That(_painFlash.PendingPredictionCount, Is.Zero);
                    Assert.That(GetOverlay().CurrentPain,
                        Is.GreaterThan(0f).And.LessThanOrEqualTo(painAfterOldConfirmation));
                }
            });
        }
        finally
        {
            await DetachPlayer();
        }
    }

    private async Task<(EntityUid OldBody, EntityUid CurrentBody, NetEntity OldBodyNet, NetEntity CurrentBodyNet)> SpawnBodies()
    {
        await Pair.CreateTestMap();

        EntityUid oldBody = default;
        EntityUid currentBody = default;
        NetEntity oldBodyNet = default;
        NetEntity currentBodyNet = default;
        await Server.WaitPost(() =>
        {
            oldBody = SSpawnAtPosition("MobHuman", TestMap!.GridCoords);
            currentBody = SSpawnAtPosition("MobVox", TestMap.GridCoords);
            oldBodyNet = SEntMan.GetNetEntity(oldBody);
            currentBodyNet = SEntMan.GetNetEntity(currentBody);
            Server.PlayerMan.SetAttachedEntity(ServerSession!, oldBody);
        });

        await RunTicksSync(5);
        await Client.WaitAssertion(() =>
            Assert.That(CEntMan.GetNetEntity(_player.LocalEntity), Is.EqualTo(oldBodyNet)));

        return (oldBody, currentBody, oldBodyNet, currentBodyNet);
    }

    private async Task DetachPlayer()
    {
        await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession!, null));
        await RunTicksSync(2);
    }

    private async Task ConfigurePainFlash()
    {
        await OverrideCVar(Side.Client, ArcaneCVars.PainFlashEnabled, true);
        await OverrideCVar(Side.Client, ArcaneCVars.PainFlashIntensity, 1f);
    }

    private static void RaiseDamage(IEntityManager entityManager, EntityUid body)
    {
        var damage = new DamageSpecifier
        {
            DamageDict =
            {
                ["Blunt"] = ExpectedPain,
                ["Heat"] = -5,
            },
        };
        var message = new DamageChangedEvent(
            entityManager.GetComponent<DamageableComponent>(body),
            damage,
            interruptsDoAfters: false,
            origin: null);
        entityManager.EventBus.RaiseLocalEvent(body, message);
    }

    private PainFlashOverlay GetOverlay()
    {
        Assert.That(_overlays.TryGetOverlay<PainFlashOverlay>(out var overlay), Is.True);
        return overlay!;
    }
}
