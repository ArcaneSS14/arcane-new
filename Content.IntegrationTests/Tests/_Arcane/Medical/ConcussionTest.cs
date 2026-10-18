#nullable enable
using System.Linq;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Common.Medical.Concussion;
using Content.Arcane.Shared.Medical.Concussion;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Damage.Components;
using Content.Shared.Inventory;
using Content.Shared.Movement.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.Speech.Components;
using Content.Shared.StatusEffectNew;
using Content.Trauma.Common.Language;
using Content.Trauma.Shared.Language.Events;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using ClientConcussion = Content.Arcane.Client.Medical.Concussion.ConcussionSystem;
using ServerConcussion = Content.Arcane.Server.Medical.Concussion.ConcussionSystem;
using ConcussionOverlay = Content.Arcane.Client.Medical.Concussion.ConcussionOverlay;

namespace Content.IntegrationTests.Tests._Arcane.Medical;

[TestOf(typeof(ServerConcussion))]
public sealed class ConcussionTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly ServerConcussion _concussion = null!;
    [SidedDependency(Side.Server)] private readonly StatusEffectsSystem _effects = null!;
    [SidedDependency(Side.Server)] private readonly InventorySystem _inventory = null!;
    [SidedDependency(Side.Server)] private readonly IPrototypeManager _prototypes = null!;
    [SidedDependency(Side.Client)] private readonly ClientConcussion _presentation = null!;
    [SidedDependency(Side.Client)] private readonly IOverlayManager _overlays = null!;
    [SidedDependency(Side.Client)] private readonly IConfigurationManager _cfg = null!;
    private static readonly EntProtoId Speech = "StatusEffectConcussedSpeech";

    [Test]
    public async Task StateEffectsCapsAndRecoveryPreserveOtherCauses()
    {
        EntityUid patient = default;
        var language = string.Empty;
        await Server.WaitPost(() =>
        {
            patient = SSpawn("MobHuman");
            var comp = SComp<ConcussionThresholdComponent>(patient);
            SEntMan.EnsureComponent<SlurredAccentComponent>(patient);
            _concussion.AddConcussionDamage((patient, comp), 1000);
            Assert.That(comp.StoredDamage.Int(), Is.EqualTo(100));
            Assert.That(comp.CurrentState, Is.EqualTo(ConcussionState.Overwhelmed));
            Assert.That(_effects.TryGetStatusEffect(patient, Speech, out _), Is.True);
            var speed = new RefreshMovementSpeedModifiersEvent();
            SEntMan.EventBus.RaiseLocalEvent(patient, speed);
            Assert.That(speed.WalkSpeedModifier, Is.EqualTo(0.7f).Within(0.001f));

            language = _prototypes.EnumeratePrototypes<LanguagePrototype>().First().ID;
            var impaired = new DetermineEntityLanguagesEvent();
            impaired.UnderstoodLanguages.Add(language);
            SEntMan.EventBus.RaiseLocalEvent(patient, ref impaired);
            Assert.That(impaired.UnderstoodLanguages, Is.Empty);
            _concussion.SetDamage((patient, comp), 0);
        });
        await RunSeconds(0.1f);
        await Server.WaitPost(() =>
        {
            var comp = SComp<ConcussionThresholdComponent>(patient);
            Assert.That(SHasComp<ConcussedComponent>(patient), Is.False);
            Assert.That(_effects.TryGetStatusEffect(patient, Speech, out _), Is.False);
            Assert.That(SHasComp<SlurredAccentComponent>(patient), Is.True);
            var restored = new DetermineEntityLanguagesEvent();
            restored.UnderstoodLanguages.Add(language);
            SEntMan.EventBus.RaiseLocalEvent(patient, ref restored);
            Assert.That(restored.UnderstoodLanguages, Does.Contain(new ProtoId<LanguagePrototype>(language)));
            SEntMan.EnsureComponent<GodmodeComponent>(patient);
            _concussion.AddConcussionDamage((patient, comp), 65);
            Assert.That(comp.StoredDamage.Int(), Is.Zero);
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void EquippedResistanceCombinesAcrossSlots()
    {
        var patient = SSpawn("MobHuman");
        Assert.That(_inventory.TryEquip(patient, SSpawn("ClothingHeadHelmetSwat"), "head", silent: true, force: true), Is.True);
        Assert.That(_inventory.TryEquip(patient, SSpawn("ClothingHeadsetAltMedical"), "ears", silent: true, force: true), Is.True);
        Assert.That(_inventory.TryEquip(patient, SSpawn("ClothingHeadsetAltMedical"), "pocket1", silent: true, force: true), Is.True);
        Assert.That(_inventory.TryEquip(patient, SSpawn("ClothingHeadsetAltMedical"), "pocket2", silent: true, force: true), Is.True);
        var dose = new FlashbangConcussionEvent(100);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref dose);
        var comp = SComp<ConcussionThresholdComponent>(patient);
        Assert.That(comp.StoredDamage.Float(), Is.EqualTo(48f).Within(0.01f));
        Assert.That(comp.CurrentState, Is.EqualTo(ConcussionState.Minor));
    }

    [Test]
    public async Task NaturalRecoveryAndRejuvenationClearConcussion()
    {
        await Pair.CreateTestMap();
        EntityUid patient = default;
        await Server.WaitPost(() =>
        {
            patient = SSpawnAtPosition("MobHuman", TestMap!.GridCoords);
            _concussion.AddConcussionDamage((patient, SComp<ConcussionThresholdComponent>(patient)), 65);
        });
        await RunSeconds(22.5f);
        await Server.WaitPost(() =>
        {
            var comp = SComp<ConcussionThresholdComponent>(patient);
            Assert.That(comp.StoredDamage.Int(), Is.Zero);
            Assert.That(comp.CurrentState, Is.EqualTo(ConcussionState.Sane));
            Assert.That(_effects.TryGetStatusEffect(patient, Speech, out _), Is.False);
            _concussion.AddConcussionDamage((patient, comp), 65);
            var rejuvenate = new RejuvenateEvent();
            SEntMan.EventBus.RaiseLocalEvent(patient, rejuvenate);
            Assert.That(comp.CurrentState, Is.EqualTo(ConcussionState.Sane));
            Assert.That(SHasComp<ConcussedComponent>(patient), Is.False);
        });
    }

    [Test]
    public async Task ChangingBodyAndRemovingTheComponentDoNotLeakEffects()
    {
        await Pair.CreateTestMap();
        EntityUid patient = default;
        EntityUid healthy = default;
        var originalSound = true;
        await Client.WaitPost(() =>
        {
            originalSound = _cfg.GetCVar(ArcaneCVars.ConcussionSound);
            _cfg.SetCVar(ArcaneCVars.ConcussionSound, false);
        });
        try
        {
            await Server.WaitPost(() =>
            {
                patient = SSpawnAtPosition("MobHuman", TestMap!.GridCoords);
                healthy = SSpawnAtPosition("MobHuman", TestMap.GridCoords);
                var comp = SComp<ConcussionThresholdComponent>(patient);
                comp.HealRate = 0;
                _concussion.AddConcussionDamage((patient, comp), 80);
                SEntMan.EnsureComponent<SlurredAccentComponent>(patient);
                Server.PlayerMan.SetAttachedEntity(ServerSession, patient);
            });
            await RunSeconds(0.2f);
            await AssertOverlay(true);
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, healthy));
            await RunSeconds(0.2f);
            await AssertOverlay(false);
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, patient));
            await RunSeconds(0.2f);
            await AssertOverlay(true);
            await Server.WaitPost(() => SEntMan.RemoveComponent<ConcussionThresholdComponent>(patient));
            await RunSeconds(0.2f);
            await AssertOverlay(false);
            await Server.WaitPost(() =>
            {
                Assert.That(_effects.TryGetStatusEffect(patient, Speech, out _), Is.False);
                Assert.That(SHasComp<SlurredAccentComponent>(patient), Is.True);
            });
        }
        finally
        {
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, null));
            await Client.WaitPost(() => _cfg.SetCVar(ArcaneCVars.ConcussionSound, originalSound));
        }
    }

    private Task AssertOverlay(bool expected) => Client.WaitPost(() =>
    {
        _presentation.FrameUpdate(0f);
        Assert.That(_overlays.HasOverlay<ConcussionOverlay>(), Is.EqualTo(expected));
    });
}
