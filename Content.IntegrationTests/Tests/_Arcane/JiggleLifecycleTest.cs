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
}
