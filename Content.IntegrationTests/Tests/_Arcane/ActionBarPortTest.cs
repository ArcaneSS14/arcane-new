#nullable enable
using System.Linq;
using Content.Arcane.Client.Actions;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Players;
using Content.Shared.Actions.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class ActionBarPortTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";

    [Test]
    public async Task ServerCapsPinsAndReturnsItsAuthoritativeSet()
    {
        EntProtoId[] candidates = [];
        EntProtoId[] actions = [];
        await Client.WaitPost(() =>
        {
            candidates = Client.ResolveDependency<IPrototypeManager>().EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract
                    && p.TryGetComponent<ActionComponent>(out _, CEntMan.ComponentFactory)
                    && p.TryGetComponent<SpriteComponent>(out _, CEntMan.ComponentFactory))
                .Select(p => new EntProtoId(p.ID)).ToArray();
        });
        await Server.WaitPost(() =>
        {
            actions = Server.ResolveDependency<IPrototypeManager>().EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract && candidates.Contains(p.ID)
                    && p.TryGetComponent<ActionComponent>(out _, SEntMan.ComponentFactory))
                .OrderBy(p => p.ID, StringComparer.Ordinal).Take(33).Select(p => new EntProtoId(p.ID)).ToArray();
        });
        Assert.That(actions, Has.Length.EqualTo(33));
        await Client.WaitPost(() =>
        {
            foreach (var action in actions)
                CEntMan.System<PinnedActionsSystem>().SetPinned(action, true);
        });
        await FlushPinRequest();
        EntProtoId[] accepted = [];
        await Server.WaitAssertion(() =>
        {
            accepted = ServerSession!.ContentData()!.PinnedActions.Keys.ToArray();
            Assert.That(accepted, Has.Length.EqualTo(32));
        });
        await Client.WaitAssertion(() =>
        {
            Assert.That(CEntMan.System<PinnedActionsSystem>().GetPinned(), Is.EquivalentTo(accepted));
        });
        await Client.WaitPost(() =>
        {
            foreach (var action in CEntMan.System<PinnedActionsSystem>().GetPinned())
                CEntMan.System<PinnedActionsSystem>().SetPinned(action, false);
        });
        await FlushPinRequest();
        await Server.WaitAssertion(() => Assert.That(ServerSession!.ContentData()!.PinnedActions, Is.Empty));
    }

    private async Task FlushPinRequest()
    {
        // PVS catch-up alone can advance no ticks. Flush client -> server -> client before checking a reply.
        await Pair.ReallyBeIdle(2);
        await Pair.RunUntilSynced();
    }
}
