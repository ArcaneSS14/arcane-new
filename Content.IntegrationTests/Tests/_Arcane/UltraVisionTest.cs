#nullable enable
using Content.Arcane.Client.Vision;
using Content.Arcane.Shared.Vision;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Mind;
using Content.Trauma.Common.CCVar;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class UltraVisionTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";
    public override PoolSettings PoolSettings => new() { Connected = true, DummyTicker = false, Dirty = true };

    [Test]
    public async Task AccessibilityChangesOnlyEnableTheFilterForTheAttachedBodyWithTheTrait()
    {
        var configuration = Client.ResolveDependency<IConfigurationManager>();
        var overlays = Client.ResolveDependency<IOverlayManager>();
        await Client.WaitAssertion(() =>
        {
            configuration.SetCVar(TraumaCVars.NoVisionFilters, true);
            configuration.SetCVar(TraumaCVars.NoVisionFilters, false);
            Assert.That(overlays.HasOverlay<UltraVisionOverlay>(), Is.False);
        });
        await Server.WaitPost(() => SEntMan.EnsureComponent<UltraVisionComponent>(SPlayer));
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(overlays.HasOverlay<UltraVisionOverlay>(), Is.True);
            configuration.SetCVar(TraumaCVars.NoVisionFilters, true);
            Assert.That(overlays.HasOverlay<UltraVisionOverlay>(), Is.False);
            configuration.SetCVar(TraumaCVars.NoVisionFilters, false);
            Assert.That(overlays.HasOverlay<UltraVisionOverlay>(), Is.True);
        });

        await Server.WaitPost(() => SEntMan.RemoveComponent<UltraVisionComponent>(SPlayer));
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(overlays.HasOverlay<UltraVisionOverlay>(), Is.False);
            configuration.SetCVar(TraumaCVars.NoVisionFilters, true);
            configuration.SetCVar(TraumaCVars.NoVisionFilters, false);
            Assert.That(overlays.HasOverlay<UltraVisionOverlay>(), Is.False,
                "Re-enabling vision filters must not grant UltraVision to a body without the trait.");
        });
    }

    [Test]
    public async Task BodyChangesRemoveAndRestoreTheFilter()
    {
        var overlays = Client.ResolveDependency<IOverlayManager>();
        var configuration = Client.ResolveDependency<IConfigurationManager>();
        await Client.WaitPost(() => configuration.SetCVar(TraumaCVars.NoVisionFilters, false));
        EntityUid mind = default;
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<UltraVisionComponent>(SPlayer);
            var minds = SEntMan.System<SharedMindSystem>();
            mind = minds.CreateMind(ServerSession!.UserId);
            minds.TransferTo(mind, SPlayer);
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() => Assert.That(overlays.HasOverlay<UltraVisionOverlay>(), Is.True));

        await Server.WaitPost(() =>
        {
            var other = SEntMan.SpawnEntity("MobHuman", SEntMan.GetCoordinates(PlayerCoords));
            SEntMan.System<SharedMindSystem>().TransferTo(mind, other);
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() => Assert.That(overlays.HasOverlay<UltraVisionOverlay>(), Is.False));
        await Server.WaitPost(() => SEntMan.System<SharedMindSystem>().TransferTo(mind, SPlayer));
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() => Assert.That(overlays.HasOverlay<UltraVisionOverlay>(), Is.True));
        await Server.WaitPost(() => SEntMan.System<SharedMindSystem>().WipeMind(mind));
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() => Assert.That(overlays.HasOverlay<UltraVisionOverlay>(), Is.False));
    }
}
