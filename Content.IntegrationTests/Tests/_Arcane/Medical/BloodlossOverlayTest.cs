using Content.Arcane.Client.Medical.Overlays;
using Content.Client.Overlays;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Body.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Robust.Client.Graphics;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane.Medical;

[TestOf(typeof(BloodlossOverlaySystem))]
public sealed class BloodlossOverlayTest : GameTest
{
    [SidedDependency(Side.Client)] private readonly BlackAndWhiteOverlaySystem _greyscale = null!;
    [SidedDependency(Side.Client)] private readonly BloodlossOverlaySystem _bloodloss = null!;
    [SidedDependency(Side.Client)] private readonly IOverlayManager _overlays = null!;
    [SidedDependency(Side.Server)] private readonly DamageableSystem _damage = null!;

    [Test]
    [RunOnSide(Side.Client)]
    public void EquipmentAndMedicalEffectsShareOneOverlay()
    {
        try
        {
            _greyscale.SetIntensity(_bloodloss, 0.4f);
            AssertIntensity(0.4f);
            _greyscale.SetIntensity(_greyscale, 1f);
            AssertIntensity(1f);
            _greyscale.SetIntensity(_bloodloss, 0.7f);
            AssertIntensity(1f);

            _greyscale.SetIntensity(_greyscale, 0f);
            AssertIntensity(0.7f);
            _greyscale.SetIntensity(_bloodloss, 0f);
            Assert.That(_overlays.HasOverlay<BlackAndWhiteOverlay>(), Is.False);
        }
        finally
        {
            _greyscale.SetIntensity(_greyscale, 0f);
            _greyscale.SetIntensity(_bloodloss, 0f);
        }
    }

    [Test]
    public async Task DamageRecoveryAndChangingPatientRefreshTheEffect()
    {
        await Pair.CreateTestMap();
        EntityUid patient = default;
        EntityUid healthy = default;
        await Server.WaitPost(() =>
        {
            patient = SSpawnAtPosition("MobHuman", TestMap!.GridCoords);
            healthy = SSpawnAtPosition("MobHuman", TestMap.GridCoords);
            SComp<BloodstreamComponent>(patient).BloodlossHealDamage = new DamageSpecifier();
            Server.PlayerMan.SetAttachedEntity(ServerSession, patient);
        });

        try
        {
            await RunSeconds(0.2f);
            await AssertNoMedicalOverlay();

            await Server.WaitPost(() =>
            {
                _damage.TryChangeDamage(patient,
                    new DamageSpecifier { DamageDict = { ["Bloodloss"] = 20, ["Asphyxiation"] = 30 } },
                    ignoreResistances: true);
            });
            await RunSeconds(0.2f);
            await Client.WaitPost(() =>
            {
                _bloodloss.FrameUpdate(0f);
                AssertIntensity(0.5f);
            });

            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, healthy));
            await RunSeconds(0.2f);
            await AssertNoMedicalOverlay();

            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, patient));
            await RunSeconds(0.2f);
            await Client.WaitPost(() =>
            {
                _bloodloss.FrameUpdate(0f);
                AssertIntensity(0.5f);
            });

            await Server.WaitPost(() => _damage.SetAllDamage(patient, 0));
            await RunSeconds(0.2f);
            await AssertNoMedicalOverlay();
        }
        finally
        {
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, null));
            await RunSeconds(0.2f);
            await AssertNoMedicalOverlay();
        }
    }

    private Task AssertNoMedicalOverlay()
    {
        return Client.WaitPost(() =>
        {
            _bloodloss.FrameUpdate(0f);
            Assert.That(_overlays.HasOverlay<BlackAndWhiteOverlay>(), Is.False);
        });
    }

    private void AssertIntensity(float intensity)
    {
        Assert.That(_overlays.TryGetOverlay<BlackAndWhiteOverlay>(out var overlay), Is.True);
        Assert.That(overlay!.Intensity, Is.EqualTo(intensity).Within(0.001f));
    }
}
