using Content.Arcane.Common.Thunderdome;
using Content.Arcane.Server.Thunderdome;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.GameTicking;
using Content.Server.Ghost;
using Content.Shared.Ghost.Components;
using Content.Shared.Mind;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P19ThunderdomeRuntimeTest : InteractionTest
{
    [Test]
    public async Task ArenaUsesExistingMapAndReturnsTheOriginalMind()
    {
        await Server.WaitAssertion(() =>
        {
            var session = ServerSession!;
            var arena = SEntMan.System<ThunderdomeSystem>();
            var minds = SEntMan.System<SharedMindSystem>();
            var ghosts = SEntMan.System<GhostSystem>();
            var position = MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));
            var body = SEntMan.SpawnAttachedTo("MobHuman", position);
            var original = minds.CreateMind(session.UserId, "Original arena player");
            minds.TransferTo(original.Owner, body, mind: original.Comp);
            Assert.That(ghosts.SpawnGhost(original.AsNullable(), position, canReturn: true), Is.Not.Null);

            Assert.That(arena.TryGetArena(out var rule), Is.True);
            var arenaSpawn = arena.PickSpawn(rule);
            Assert.That(arenaSpawn, Is.Not.Null);
            var normalSpawn = SEntMan.System<ServerGameTicker>().GetObserverSpawnPoint();
            Assert.That(normalSpawn.GetMapId(SEntMan), Is.Not.EqualTo(arenaSpawn!.Value.GetMapId(SEntMan)));
            Assert.That(arena.TryJoin(session, rule, -1), Is.False);
            Assert.That(arena.TryJoin(session, rule, 0), Is.True);
            var avatar = session.AttachedEntity!.Value;
            Assert.That(SEntMan.HasComponent<ThunderdomeParticipantComponent>(avatar), Is.True);
            Assert.That(minds.TryGetMind(session.UserId, out var temporary), Is.True);
            Assert.That(temporary!.Value.Owner, Is.Not.EqualTo(original.Owner));
            Assert.That(original.Comp.OwnedEntity, Is.EqualTo(body));

            Assert.That(ghosts.OnGhostAttempt(temporary.Value.Owner, canReturnGlobal: true), Is.True);
            Assert.That(minds.TryGetMind(session.UserId, out var restored), Is.True);
            Assert.That(restored!.Value.Owner, Is.EqualTo(original.Owner));
            Assert.That(SEntMan.HasComponent<GhostComponent>(session.AttachedEntity!.Value), Is.True);
            Assert.That(original.Comp.OwnedEntity, Is.EqualTo(body));
            minds.WipeMind(original.Owner, original.Comp);
        });
    }
}
