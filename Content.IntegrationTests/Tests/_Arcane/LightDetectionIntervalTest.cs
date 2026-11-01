#nullable enable
using Content.Goobstation.Common.CCVar;
using Content.Goobstation.Shared.LightDetection.Components;
using Content.IntegrationTests.Tests.Interaction;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class LightDetectionIntervalTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task LightDetectionContinuesUpdatingAfterConfiguredIntervals()
    {
        EntityUid lamp = default;
        await Server.WaitPost(() =>
        {
            Server.ResolveDependency<IConfigurationManager>().SetCVar(GoobCVars.LightUpdateFrequency, 1f);
            SEntMan.EnsureComponent<LightDetectionComponent>(SPlayer);
            lamp = SEntMan.SpawnEntity(null, SEntMan.GetCoordinates(PlayerCoords));
            var light = SEntMan.EnsureComponent<PointLightComponent>(lamp);
            var lights = SEntMan.System<SharedPointLightSystem>();
            lights.SetRadius(lamp, 5f, light);
            lights.SetEnergy(lamp, 2f, light);
            lights.SetEnabled(lamp, true, light);
        });
        await Pair.RunTicksSync(45);
        await Server.WaitAssertion(() => Assert.That(SEntMan.GetComponent<LightDetectionComponent>(SPlayer).CurrentLightLevel, Is.GreaterThan(0f)));
        await Server.WaitPost(() => SEntMan.System<SharedPointLightSystem>().SetEnabled(lamp, false));
        await Pair.RunTicksSync(45);
        await Server.WaitAssertion(() => Assert.That(SEntMan.GetComponent<LightDetectionComponent>(SPlayer).CurrentLightLevel, Is.Zero));
    }
}
