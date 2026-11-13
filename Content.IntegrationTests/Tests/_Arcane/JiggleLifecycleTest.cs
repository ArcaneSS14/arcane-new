using System.Linq;
using Content.Arcane.Client.Jiggle;
using Content.Arcane.Shared.Jiggle;
using Content.Client.Graphics;
using Content.Client.Stealth;
using Content.Goobstation.Shared.Emoting;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Stealth.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class JiggleLifecycleTest : InteractionTest
{
    protected override string PlayerPrototype => "MobSlimePerson";

    [TestCase(true)]
    [TestCase(false)]
    public async Task JiggleRendersBeforeStealthRegardlessOfActivationOrder(bool stealthFirst)
    {
        await Server.WaitPost(() =>
        {
            SEntMan.EnsureComponent<StealthComponent>(SPlayer);
            var jiggle = SEntMan.GetComponent<JiggleComponent>(SPlayer);
            jiggle.SwayAmplitude = 0f;
            SEntMan.Dirty(SPlayer, jiggle);
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            CEntMan.System<JiggleSystem>().FrameUpdate(1f / 60f);
            CEntMan.System<StealthSystem>().SetEnabled(CPlayer, false);
            var sprites = CEntMan.System<SpriteSystem>();
            Assert.That(sprites.HasPostShader(CPlayer, JiggleSystem.PostShaderId), Is.False);
            Assert.That(sprites.HasPostShader(CPlayer, ContentPostShaderIds.Stealth), Is.False);
            if (stealthFirst)
                CEntMan.System<StealthSystem>().SetEnabled(CPlayer, true);
        });

        await Server.WaitPost(() => SEntMan.System<SharedAnimatedEmotesSystem>().PlayEmoteAnimation(SPlayer, "Squish"));
        var jiggleActive = false;
        for (var i = 0; i < 100 && !jiggleActive; i++)
        {
            await Pair.ReallyBeIdle(1);
            await Client.WaitPost(() =>
            {
                CEntMan.System<JiggleSystem>().FrameUpdate(1f / 60f);
                jiggleActive = CEntMan.System<SpriteSystem>().HasPostShader(CPlayer, JiggleSystem.PostShaderId);
            });
        }

        await Client.WaitAssertion(() =>
        {
            Assert.That(jiggleActive, Is.True, "The server emote must activate Jiggle before checking its render order.");
            if (!stealthFirst)
            {
                // A server snapshot can correct the local Enabled field while delivering
                // the emote. Exercise a real presentation transition in this order too.
                var stealth = CEntMan.System<StealthSystem>();
                stealth.SetEnabled(CPlayer, false);
                stealth.SetEnabled(CPlayer, true);
            }
            var ordered = CEntMan.System<SpriteSystem>().GetPostShaders(CPlayer).Select(entry => entry.Id).ToArray();
            Assert.That(ordered.Intersect(ContentPostShaderIds.BeforeOutlines), Is.Empty,
                "An outline must not accidentally supply the missing ordering constraint.");
            var jiggleIndex = Array.IndexOf(ordered, JiggleSystem.PostShaderId);
            var stealthIndex = Array.IndexOf(ordered, ContentPostShaderIds.Stealth);
            Assert.That(jiggleIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(stealthIndex, Is.GreaterThan(jiggleIndex));
        });
    }

    [Test]
    public async Task EmoteEffectsUseIndependentShadersAndRemovalCleansTheOwner()
    {
        var other = await Spawn("MobSlimePerson");
        var npc = await Spawn("MobAdultSlimesBlue");
        await Server.WaitAssertion(() =>
        {
            Assert.That(SEntMan.HasComponent<JiggleComponent>(SPlayer), Is.True);
            Assert.That(SEntMan.HasComponent<JiggleComponent>(ToServer(npc)), Is.False);
            foreach (var uid in new[] {SPlayer, ToServer(other)})
            {
                var jiggle = SEntMan.GetComponent<JiggleComponent>(uid);
                jiggle.SwayAmplitude = 0f;
                SEntMan.Dirty(uid, jiggle);
            }
        });
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(CEntMan.HasComponent<JiggleComponent>(CPlayer), Is.True);
            Assert.That(CEntMan.HasComponent<JiggleVisualsComponent>(CPlayer), Is.True);
            Assert.That(CEntMan.HasComponent<Robust.Shared.Physics.Components.PhysicsComponent>(CPlayer), Is.True);
        });
        await PulseBoth();
        ShaderInstance first = null!;
        await Client.WaitAssertion(() =>
        {
            var sprites = CEntMan.System<SpriteSystem>();
            CEntMan.System<JiggleSystem>().FrameUpdate(1f / 60f);
            Assert.That(sprites.TryGetPostShader(CPlayer, JiggleSystem.PostShaderId, out var one), Is.True);
            Assert.That(sprites.TryGetPostShader(ToClient(other), JiggleSystem.PostShaderId, out var two), Is.True);
            Assert.That(one.Shader, Is.Not.SameAs(two.Shader));
            Assert.That(sprites.HasPostShader(ToClient(npc), JiggleSystem.PostShaderId), Is.False);
            first = one.Shader;
        });

        await Server.WaitPost(() => SEntMan.RemoveComponent<JiggleComponent>(SPlayer));
        await Pair.RunUntilSynced();
        await Client.WaitAssertion(() =>
        {
            Assert.That(CEntMan.HasComponent<JiggleVisualsComponent>(CPlayer), Is.False);
            Assert.That(CEntMan.System<SpriteSystem>().HasPostShader(CPlayer, JiggleSystem.PostShaderId), Is.False);
            Assert.That(CEntMan.HasComponent<JiggleVisualsComponent>(ToClient(other)), Is.True);
        });

        await Server.WaitPost(() => SEntMan.EnsureComponent<JiggleComponent>(SPlayer));
        await Pair.RunUntilSynced();
        await PulseBoth();
        await Client.WaitAssertion(() =>
        {
            CEntMan.System<JiggleSystem>().FrameUpdate(1f / 60f);
            Assert.That(CEntMan.System<SpriteSystem>().TryGetPostShader(CPlayer, JiggleSystem.PostShaderId, out var fresh), Is.True);
            Assert.That(fresh.Shader, Is.Not.SameAs(first));
            CEntMan.RemoveComponent<JiggleVisualsComponent>(CPlayer);
            Assert.That(CEntMan.System<SpriteSystem>().HasPostShader(CPlayer, JiggleSystem.PostShaderId), Is.False);
        });

        async Task PulseBoth()
        {
            await Server.WaitPost(() =>
            {
                var emotes = SEntMan.System<SharedAnimatedEmotesSystem>();
                emotes.PlayEmoteAnimation(SPlayer, "Squish");
                emotes.PlayEmoteAnimation(ToServer(other), "Squish");
            });
            // Entity effects wait for the source game-state tick on the client. Observe the real
            // shader result while pumping both peers instead of assuming two ticks deliver it.
            for (var i = 0; i < 100; i++)
            {
                await Pair.ReallyBeIdle(1);
                var active = false;
                await Client.WaitPost(() =>
                {
                    CEntMan.System<JiggleSystem>().FrameUpdate(1f / 60f);
                    var sprites = CEntMan.System<SpriteSystem>();
                    active = sprites.HasPostShader(CPlayer, JiggleSystem.PostShaderId) &&
                             sprites.HasPostShader(ToClient(other), JiggleSystem.PostShaderId);
                });
                if (active)
                    return;
            }
            Assert.Fail("The server emote effect did not produce both client postshaders.");
        }
    }
}
