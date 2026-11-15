// Arcane-Start
#nullable enable
using Content.Arcane.Shared.Tools;
using Content.Client.Light.Components;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Audio.Components;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P25WeldingRuntimeTest : InteractionTest
{
    private static readonly ProtoId<ToolQualityPrototype> Welding = "Welding";
    private static readonly EntProtoId AnchorPrototype = "ArcaneWeldingEffectAnchor";

    [Test]
    public async Task WeldingLightHoldsAndDipsThenResets()
    {
        await Client.WaitAssertion(() =>
        {
            var light = CEntMan.SpawnAtPosition("ArcaneWeldingLight", CEntMan.GetCoordinates(TargetCoords));
            try
            {
                var pointLight = CEntMan.GetComponent<PointLightComponent>(light);
                var animation = CEntMan.GetComponent<LightBehaviourComponent>(light).Animations[0].Animation;
                var player = CEntMan.System<AnimationPlayerSystem>();
                var cycle = (float) animation.Length.TotalSeconds;
                Assert.That(cycle, Is.InRange(0.42f, 0.78f));
                var bright = pointLight.Energy;
                Assert.That(bright, Is.GreaterThan(2f));

                player.FrameUpdate(cycle - 0.09f);
                Assert.That(pointLight.Energy, Is.EqualTo(bright));

                player.FrameUpdate(0.04f);
                Assert.That(pointLight.Energy, Is.LessThan(bright / 2));

                player.FrameUpdate(0.06f);
                Assert.That(pointLight.Energy, Is.EqualTo(bright));
            }
            finally
            {
                CEntMan.DeleteEntity(light);
            }
        });
    }

    [TestCase("ClosetWall", false)]
    [TestCase("BorgChassisGeneric", true)]
    [TestCase("MobIPC", true)]
    public async Task WeldingUsesTheClickedPointOnTheTarget(string prototype, bool needsRepair)
    {
        try
        {
            await VerifyWeldingContact(prototype, needsRepair);
        }
        catch (Exception error)
        {
            TestContext.Out.WriteLine(error);
            throw;
        }
    }

    private async Task VerifyWeldingContact(string prototype, bool needsRepair)
    {
        await SpawnTarget(prototype);
        var target = STarget!.Value;
        var tool = ToServer(await PlaceInHands("WelderExperimental", enableToggleable: false));
        if (needsRepair)
        {
            await Server.WaitAssertion(() =>
            {
                var damage = new DamageSpecifier { DamageDict = { ["Blunt"] = 30 } };
                SEntMan.System<DamageableSystem>().TryChangeDamage(target, damage, ignoreResistances: true);
            });
            await RunTicks(1);
        }

        var anchor = EntityUid.Invalid;
        var audio = EntityUid.Invalid;
        TimeSpan start = default, duration = default;
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.System<ItemToggleSystem>().TryActivate(tool, SPlayer), Is.True);
            var xform = SEntMan.System<SharedTransformSystem>();
            var offset = new Vector2(0.27f, 0.19f);
            var world = xform.ToMapCoordinates(new EntityCoordinates(target, offset));
            var click = xform.ToCoordinates(MapData.Grid.Owner, world);
            var use = new InteractUsingEvent(SPlayer, tool, target, click);
            SEntMan.EventBus.RaiseLocalEvent(target, use);
            Assert.That(use.Handled, Is.True);

            anchor = FindAnchor(target);
            Assert.That(anchor, Is.Not.EqualTo(EntityUid.Invalid));
            if (SEntMan.TryGetComponent<FlammableComponent>(target, out var flammable))
                Assert.That(flammable.OnFire, Is.False);
            var position = SEntMan.GetComponent<TransformComponent>(anchor).LocalPosition;
            Assert.That(Vector2.Distance(position, offset), Is.LessThan(0.001f));
            var effect = SEntMan.GetComponent<WeldingEffectComponent>(anchor);
            start = effect.StartTime;
            duration = effect.Duration;
            audio = FindWorkAudio(anchor);
            Assert.That(audio, Is.Not.EqualTo(EntityUid.Invalid));
        });

        if (!needsRepair)
            return;

        await RunSeconds((float) duration.TotalSeconds + 0.15f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(FindAnchor(target), Is.EqualTo(anchor));
            Assert.That(FindWorkAudio(anchor), Is.EqualTo(audio));
            Assert.That(SEntMan.GetComponent<WeldingEffectComponent>(anchor).StartTime, Is.GreaterThan(start));
        });
    }

    [Test]
    public async Task WeldingFollowsTheShutterAndCleansUpOnPauseCancelAndCompletion()
    {
        try
        {
            await VerifyWeldingLifecycle();
        }
        catch (Exception error)
        {
            TestContext.Out.WriteLine(error);
            throw;
        }
    }

    private async Task VerifyWeldingLifecycle()
    {
        await SpawnTarget("ShuttersNormal");
        var shutter = STarget!.Value;
        var tool = ToServer(await PlaceInHands("WelderExperimental", enableToggleable: false));
        var doAfter = SEntMan.System<SharedDoAfterSystem>();
        var tools = SEntMan.System<SharedToolSystem>();
        var maps = SEntMan.System<SharedMapSystem>();
        DoAfterId? action = null;
        var initialAnchor = EntityUid.Invalid;
        var resumedAnchor = EntityUid.Invalid;
        var initialAudio = EntityUid.Invalid;
        var resumedAudio = EntityUid.Invalid;
        var replacementAnchor = EntityUid.Invalid;
        var replacementAudio = EntityUid.Invalid;
        var completionAudio = EntityUid.Invalid;

        await Server.WaitAssertion(() =>
        {
            var weldable = SEntMan.System<WeldableSystem>();
            Assert.That(tools.UseTool(tool, SPlayer, shutter, TimeSpan.FromSeconds(3),
                [Welding], new WeldFinishedEvent(), out _), Is.False, "An unlit tool started welding.");
            Assert.That(FindAnchor(shutter), Is.EqualTo(EntityUid.Invalid));
            Assert.That(SEntMan.System<ItemToggleSystem>().TryActivate(tool, SPlayer), Is.True);

            weldable.SetWeldedState(shutter, true);
            Assert.That(tools.UseTool(tool, SPlayer, shutter, TimeSpan.FromSeconds(3),
                [Welding], new WeldFinishedEvent(), out action), Is.True);
            Assert.That(doAfter.IsRunning(action), Is.True);
            initialAnchor = FindAnchor(shutter);
            Assert.That(initialAnchor, Is.Not.EqualTo(EntityUid.Invalid));
            initialAudio = FindWorkAudio(initialAnchor);
            Assert.That(initialAudio, Is.Not.EqualTo(EntityUid.Invalid));
            var state = SEntMan.GetComponent<WeldingEffectComponent>(initialAnchor);
            Assert.That(state.Experimental, Is.True);
            Assert.That(state.StartOffset, Is.EqualTo(new Vector2(0.5f, -0.5f)));
            Assert.That(state.EndOffset, Is.EqualTo(new Vector2(-0.5f, -0.5f)));
            Assert.That(SProtoMan.Index(AnchorPrototype).MapSavable, Is.False);

            maps.SetPaused(MapId, true);
        });

        await RunSeconds(0.2f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(initialAnchor), Is.False);
            Assert.That(SEntMan.EntityExists(initialAudio), Is.False);
            Assert.That(doAfter.IsRunning(action), Is.True);
            maps.SetPaused(MapId, false);
        });

        await RunSeconds(0.2f);
        await Server.WaitAssertion(() =>
        {
            resumedAnchor = FindAnchor(shutter);
            Assert.That(resumedAnchor, Is.Not.EqualTo(EntityUid.Invalid));
            Assert.That(resumedAnchor, Is.Not.EqualTo(initialAnchor));
            resumedAudio = FindWorkAudio(resumedAnchor);
            Assert.That(resumedAudio, Is.Not.EqualTo(EntityUid.Invalid));
            SEntMan.DeleteEntity(resumedAnchor);
        });

        await RunSeconds(0.2f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(resumedAudio), Is.False);
            replacementAnchor = FindAnchor(shutter);
            Assert.That(replacementAnchor, Is.Not.EqualTo(EntityUid.Invalid));
            replacementAudio = FindWorkAudio(replacementAnchor);
            Assert.That(replacementAudio, Is.Not.EqualTo(EntityUid.Invalid));
            doAfter.Cancel(action);
        });

        await RunSeconds(0.2f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(replacementAnchor), Is.False);
            Assert.That(SEntMan.EntityExists(replacementAudio), Is.False);
            Assert.That(SEntMan.System<WeldableSystem>().IsWelded(shutter), Is.True);
            Assert.That(tools.UseTool(tool, SPlayer, shutter, TimeSpan.FromSeconds(0.5),
                [Welding], new WeldFinishedEvent(), out action), Is.True);
            Assert.That(doAfter.IsRunning(action), Is.True);
            var anchor = FindAnchor(shutter);
            Assert.That(anchor, Is.Not.EqualTo(EntityUid.Invalid));
            completionAudio = FindWorkAudio(anchor);
            Assert.That(completionAudio, Is.Not.EqualTo(EntityUid.Invalid));
        });
        await RunSeconds(0.8f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(FindAnchor(shutter), Is.EqualTo(EntityUid.Invalid));
            Assert.That(SEntMan.EntityExists(completionAudio), Is.False);
            Assert.That(SEntMan.System<WeldableSystem>().IsWelded(shutter), Is.False);
            Assert.That(tools.UseTool(tool, SPlayer, shutter, TimeSpan.Zero,
                [Welding], new WeldFinishedEvent(), out _), Is.True);
            Assert.That(FindAnchor(shutter), Is.EqualTo(EntityUid.Invalid));
        });
    }

    private EntityUid FindAnchor(EntityUid parent)
    {
        var query = SEntMan.AllEntityQueryEnumerator<WeldingEffectComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var transform))
        {
            if (transform.ParentUid == parent)
                return uid;
        }

        return EntityUid.Invalid;
    }

    private EntityUid FindWorkAudio(EntityUid anchor)
    {
        var query = SEntMan.AllEntityQueryEnumerator<AudioComponent, TransformComponent>();
        var stream = EntityUid.Invalid;
        while (query.MoveNext(out var uid, out var audio, out var transform))
        {
            if (transform.ParentUid != anchor || !audio.Params.Loop)
                continue;

            Assert.That(stream, Is.EqualTo(EntityUid.Invalid), "Only one loop can belong to the weld.");
            stream = uid;
        }

        return stream;
    }
}
// Arcane-End
