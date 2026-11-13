// Arcane-Start
#nullable enable
using Content.Arcane.Shared.Tools;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.DoAfter;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Tools;
using Content.Shared.Tools.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P25WeldingRuntimeTest : InteractionTest
{
    private static readonly ProtoId<ToolQualityPrototype> Welding = "Welding";
    private static readonly EntProtoId AnchorPrototype = "ArcaneWeldingEffectAnchor";

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
            Assert.That(doAfter.IsRunning(action), Is.True);
            maps.SetPaused(MapId, false);
        });

        await RunSeconds(0.2f);
        await Server.WaitAssertion(() =>
        {
            resumedAnchor = FindAnchor(shutter);
            Assert.That(resumedAnchor, Is.Not.EqualTo(EntityUid.Invalid));
            Assert.That(resumedAnchor, Is.Not.EqualTo(initialAnchor));
            doAfter.Cancel(action);
        });

        await RunSeconds(0.2f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.EntityExists(resumedAnchor), Is.False);
            Assert.That(SEntMan.System<WeldableSystem>().IsWelded(shutter), Is.True);
            Assert.That(tools.UseTool(tool, SPlayer, shutter, TimeSpan.FromSeconds(0.5),
                [Welding], new WeldFinishedEvent(), out action), Is.True);
            Assert.That(doAfter.IsRunning(action), Is.True);
            Assert.That(FindAnchor(shutter), Is.Not.EqualTo(EntityUid.Invalid));
        });
        await RunSeconds(0.8f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(FindAnchor(shutter), Is.EqualTo(EntityUid.Invalid));
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
}
// Arcane-End
