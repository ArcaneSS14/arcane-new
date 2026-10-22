#nullable enable
using System.Linq;
using System.Numerics;
using Content.Arcane.Common.CCVar;
using Content.Arcane.Common.Medical.Concussion;
using Content.Arcane.Shared.Medical.Concussion;
using Content.Goobstation.Shared.Flashbang;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Inventory;
using Content.Shared.Flash;
using Content.Shared.Speech.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.Trigger.Components.Effects;
using Content.Trauma.Common.Language.Systems;
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
    [SidedDependency(Side.Server)] private readonly SharedFlashSystem _flash = null!;
    [SidedDependency(Side.Server)] private readonly CommonLanguageSystem _languages = null!;
    [SidedDependency(Side.Client)] private readonly ClientConcussion _presentation = null!;
    [SidedDependency(Side.Client)] private readonly IOverlayManager _overlays = null!;
    [SidedDependency(Side.Client)] private readonly IConfigurationManager _cfg = null!;
    [SidedDependency(Side.Client)] private readonly Content.Client.Audio.ContentAudioSystem _clientAudio = null!;
    [SidedDependency(Side.Client)] private readonly Robust.Client.Audio.AudioSystem _audio = null!;
    private static readonly EntProtoId Speech = "StatusEffectConcussedSpeech";

    [Test]
    public async Task FlashbangRecoveryPreservesUnrelatedSpeechAndRestoresLanguage()
    {
        await Pair.CreateTestMap();
        EntityUid patient = default;
        var language = CommonLanguageSystem.FallbackLanguagePrototype;
        var recoveryTime = 0f;
        await Server.WaitPost(() =>
        {
            patient = SSpawnAtPosition("MobHuman", TestMap!.GridCoords);
            var comp = SComp<ConcussionThresholdComponent>(patient);
            SEntMan.EnsureComponent<SlurredAccentComponent>(patient);
            var grenade = SSpawnAtPosition("GrenadeFlashBang", TestMap.GridCoords);
            var flash = SComp<FlashOnTriggerComponent>(grenade);
            _flash.FlashArea(grenade, null, flash.Range, flash.Duration);
            var cap = comp.Thresholds.First(entry => entry.Value == ConcussionState.Overwhelmed).Key.Float();
            Assert.That(comp.StoredDamage.Float(), Is.EqualTo(Math.Min(cap, SComp<FlashbangComponent>(grenade).ConcussionDamage)).Within(0.01f));
            Assert.That(_effects.TryGetStatusEffect(patient, Speech, out _), Is.True);

            var impaired = new DetermineEntityLanguagesEvent();
            impaired.UnderstoodLanguages.Add(language);
            SEntMan.EventBus.RaiseLocalEvent(patient, ref impaired);
            Assert.That(impaired.UnderstoodLanguages, Is.Empty);
            Assert.That(_languages.CanUnderstand(patient, language), Is.False);
            Assert.That(_languages.CanUnderstand(patient, CommonLanguageSystem.UniversalPrototype), Is.True);
            Assert.That(_languages.CanUnderstand(patient, CommonLanguageSystem.PsychomanticPrototype), Is.True);
            var minor = comp.Thresholds.First(entry => entry.Value == ConcussionState.Minor).Key;
            _concussion.SetDamage((patient, comp), minor + comp.HealRate * (float)comp.UpdateInterval.TotalSeconds);
            recoveryTime = (float)comp.UpdateInterval.TotalSeconds * 3f;
        });
        await RunSeconds(recoveryTime);
        await Server.WaitPost(() =>
        {
            Assert.That(SHasComp<ConcussedComponent>(patient), Is.False);
            Assert.That(_effects.TryGetStatusEffect(patient, Speech, out _), Is.False);
            Assert.That(SHasComp<SlurredAccentComponent>(patient), Is.True);
            var restored = new DetermineEntityLanguagesEvent();
            restored.UnderstoodLanguages.Add(language);
            SEntMan.EventBus.RaiseLocalEvent(patient, ref restored);
            Assert.That(restored.UnderstoodLanguages, Does.Contain(language));
        });
    }

    [Test]
    [RunOnSide(Side.Server)]
    public void EquippedResistanceCombinesAcrossSlots()
    {
        var patient = SSpawn("MobHuman");
        var comp = SComp<ConcussionThresholdComponent>(patient);
        Assert.That(_inventory.TryEquip(patient, SSpawn("ClothingHeadHelmetSwat"), "head", silent: true, force: true), Is.True);
        var dose = new FlashbangConcussionEvent(comp.Thresholds.First(entry => entry.Value == ConcussionState.Overwhelmed).Key.Float() / 2f);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref dose);
        var helmetOnly = comp.StoredDamage;
        _concussion.SetDamage((patient, comp), 0);
        var headset = SSpawn("ClothingHeadsetAltMedical");
        Assert.That(_inventory.TryEquip(patient, headset, "ears", silent: true, force: true), Is.True);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref dose);
        var wornProtection = comp.StoredDamage;
        var resistance = Math.Clamp(SComp<ConcussionResistanceComponent>(headset).Resistance, 0f, 1f);
        Assert.That(wornProtection.Float(), Is.EqualTo(helmetOnly.Float() * (1f - resistance)).Within(0.01f));
        _concussion.SetDamage((patient, comp), 0);
        Assert.That(_inventory.TryEquip(patient, SSpawn("ClothingHeadsetAltMedical"), "pocket1", silent: true, force: true), Is.True);
        Assert.That(_inventory.TryEquip(patient, SSpawn("ClothingHeadsetAltMedical"), "pocket2", silent: true, force: true), Is.True);
        SEntMan.EventBus.RaiseLocalEvent(patient, ref dose);
        Assert.That(comp.StoredDamage, Is.EqualTo(wornProtection));
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
                _concussion.AddConcussionDamage((patient, comp), comp.Thresholds.First(entry => entry.Value == ConcussionState.Overwhelmed).Key);
                _effects.TryUpdateStatusEffectDuration(patient, SharedFlashSystem.FlashedKey, TimeSpan.FromMinutes(1));
                SEntMan.EnsureComponent<SlurredAccentComponent>(patient);
                Server.PlayerMan.SetAttachedEntity(ServerSession, patient);
            });
            await RunSeconds(0.2f);
            await AssertOverlays(true, true);
            await Client.WaitPost(() =>
            {
                Assert.That(_clientAudio.ListenerOcclusion, Is.EqualTo(float.PositiveInfinity));
                Assert.That(_audio.GetOcclusion(_audio.GetListenerCoordinates(), Vector2.Zero, 0f),
                    Is.EqualTo(float.PositiveInfinity));
            });
            await Server.WaitPost(() =>
            {
                var comp = SComp<ConcussionThresholdComponent>(patient);
                var minor = comp.Thresholds.First(entry => entry.Value == ConcussionState.Minor).Key.Float();
                var cap = comp.Thresholds.First(entry => entry.Value == ConcussionState.Overwhelmed).Key.Float();
                _concussion.SetDamage((patient, comp), minor + (cap - minor) * comp.HearingRecoveryThreshold * 0.5f);
            });
            await RunSeconds(0.2f);
            await AssertOverlays(true, true);
            await Client.WaitPost(() =>
            {
                Assert.That(float.IsFinite(_clientAudio.ListenerOcclusion), Is.True);
                Assert.That(_clientAudio.ListenerOcclusion, Is.GreaterThan(0f));
                Assert.That(_audio.GetOcclusion(_audio.GetListenerCoordinates(), Vector2.Zero, 0f),
                    Is.GreaterThanOrEqualTo(_clientAudio.ListenerOcclusion));
            });
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, healthy));
            await RunSeconds(0.2f);
            await AssertOverlays(false, false);
            await Server.WaitPost(() => Server.PlayerMan.SetAttachedEntity(ServerSession, patient));
            await RunSeconds(0.2f);
            await AssertOverlays(true, true);
            await Server.WaitPost(() => SEntMan.RemoveComponent<ConcussionThresholdComponent>(patient));
            await RunSeconds(0.2f);
            await AssertOverlays(false, true);
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
        await RunSeconds(0.2f);
        await AssertOverlays(false, false);
    }

    private Task AssertOverlays(bool concussion, bool flash) => Client.WaitPost(() =>
    {
        _presentation.FrameUpdate(0f);
        Assert.That(_overlays.HasOverlay<ConcussionOverlay>(), Is.EqualTo(concussion));
        Assert.That(_overlays.HasOverlay<Content.Client.Flash.FlashOverlay>(), Is.EqualTo(flash));
        Assert.That(_clientAudio.ListenerOcclusion > 0f, Is.EqualTo(concussion));
    });
}
