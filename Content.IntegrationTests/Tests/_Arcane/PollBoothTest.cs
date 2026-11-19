#nullable enable
using System.Linq;
using Content.Arcane.Client.Polls;
using Content.Arcane.Shared.Polls;
using Content.Goobstation.Client.Polls;
using Content.Goobstation.Client.Polls.UI;
using Content.Goobstation.Shared.Polls;
using Content.IntegrationTests.Tests.Interaction;
using Content.Server.Database;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.GameObjects;
using Robust.Shared.Localization;
using ClientPollManager = Content.Goobstation.Client.Polls.PollManager;
using ServerPollManager = Content.Goobstation.Server.Polls.PollManager;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class PollBoothTest : InteractionTest
{
    protected override string PlayerPrototype => "MobHuman";
    public override PoolSettings PoolSettings => new() { Connected = true, DummyTicker = false, Dirty = true };

    // Expiry checks the last unseen poll across the entire database.
    private static PoolSettings ExpiryPair => new()
    {
        Connected = true,
        DummyTicker = false,
        Dirty = true,
        Fresh = true,
        Destructive = true,
    };

    [Test]
    [NonParallelizable] // The database counters are shared by all test pairs.
    public async Task OnlyCardsInTheOpenWindowCountAndViewsSurviveReconnect()
    {
        var db = Server.ResolveDependency<IServerDbManager>();
        var polls = Server.ResolveDependency<ServerPollManager>();
        var clientPolls = Client.ResolveDependency<ClientPollManager>();
        var ui = Client.ResolveDependency<IUserInterfaceManager>();
        var controller = ui.GetUIController<PollUIController>();
        EntityUid mind = default;
        await Server.WaitPost(() =>
        {
            var minds = SEntMan.System<SharedMindSystem>();
            mind = minds.CreateMind(ServerSession!.UserId);
            minds.TransferTo(mind, SPlayer);
        });
        var first = await RunServerTask(() => polls.CreatePoll("Booth test", "First poll", ["Yes", "No"], null, false));
        Assert.That(first, Is.Not.Null);
        var booth = await SpawnTarget("PollBooth");
        await ReceiveList(clientPolls.RequestActivePolls);
        Assert.That(await db.GetPollSeenCountAsync(first!.PollId), Is.Zero, "A background request is not a view.");
        await ReceiveList(() => CEntMan.System<SharedInteractionSystem>().InteractionActivate(CPlayer, ToClient(booth)));
        await WaitForView(first.PollId);
        Assert.That(await db.GetPlayerVotesAsync(first.PollId, ServerSession!.UserId), Is.Empty);
        var readsBeforeReplay = ServerDbManager.DbReadOpsMetric.Value;
        var writesBeforeReplay = ServerDbManager.DbWriteOpsMetric.Value;
        await Client.WaitPost(() =>
        {
            CEntMan.EntityNetManager.SendSystemNetworkMessage(new Content.Arcane.Shared.Polls.MarkPollSeenEvent(first.PollId));
        });
        await Pair.ReallyBeIdle(5);
        Assert.Multiple(() =>
        {
            Assert.That(ServerDbManager.DbReadOpsMetric.Value, Is.EqualTo(readsBeforeReplay),
                "Acknowledging an already confirmed view must not read the database again.");
            Assert.That(ServerDbManager.DbWriteOpsMetric.Value, Is.EqualTo(writesBeforeReplay),
                "Acknowledging an already confirmed view must not attempt another database write.");
        });
        Assert.That(await db.GetPollSeenCountAsync(first.PollId), Is.EqualTo(1));
        await Client.WaitPost(controller.CloseWindow);

        var second = await RunServerTask(() => polls.CreatePoll("Closed window", "Second poll", ["Yes", "No"], null, false));
        Assert.That(second, Is.Not.Null);
        await Client.WaitPost(() => CEntMan.EntityNetManager.SendSystemNetworkMessage(
            new Content.Arcane.Shared.Polls.MarkPollSeenEvent(second!.PollId)));
        await Pair.ReallyBeIdle(5);
        Assert.That(await db.GetPollSeenCountAsync(second!.PollId), Is.Zero, "A broadcast alone does not offer the poll as a viewed card.");
        await ReceiveList(() =>
        {
            controller.OpenWindow();
            controller.CloseWindow();
        });
        Assert.That(await db.GetPollSeenCountAsync(second!.PollId), Is.Zero, "Closing before the response must not mark cards seen.");
        await Client.WaitAssertion(() => Assert.That(CEntMan.System<PollSeenSystem>().HasUnseenPolls, Is.True));
        await ReceiveList(controller.OpenWindow);
        await WaitForView(second.PollId);
        await Client.WaitPost(controller.CloseWindow);

        var third = await RunServerTask(() => polls.CreatePoll("Unseen after reconnect", "Readiness signal", ["Yes", "No"], null, false));
        Assert.That(third, Is.Not.Null);

        var net = Client.ResolveDependency<IClientNetManager>();
        var name = ServerSession!.Name;
        await Client.WaitPost(() => net.ClientDisconnect("Poll view integration test"));
        await Pair.RunTicksSync(20);
        Client.SetConnectTarget(Server);
        await Client.WaitPost(() => net.ClientConnect(null!, 0, name));
        await Pair.RunTicksSync(20);
        await ReceiveList(clientPolls.RequestActivePolls);
        await Client.WaitAssertion(() =>
        {
            Assert.That(clientPolls.ActivePolls.ContainsKey(third!.PollId), Is.True);
            Assert.That(clientPolls.ActivePolls[first.PollId].Active, Is.True);
            Assert.That(clientPolls.ActivePolls[second.PollId].Active, Is.True);
            Assert.That(CEntMan.System<PollSeenSystem>().HasUnseenPolls, Is.True,
                "A real unseen poll proves the reconnected client has initialized its history.");
        });
        await RunServerTask(() => polls.ClosePoll(third!.PollId));
        await Pair.ReallyBeIdle(5);
        await Client.WaitAssertion(() =>
        {
            Assert.That(clientPolls.ActivePolls[third!.PollId].Active, Is.False);
            Assert.That(clientPolls.ActivePolls[first.PollId].Active, Is.True);
            Assert.That(clientPolls.ActivePolls[second.PollId].Active, Is.True);
            Assert.That(CEntMan.System<PollSeenSystem>().HasUnseenPolls, Is.False,
                "After the new poll closes, both previously viewed cards must still be recognized as seen.");
        });
        Assert.That(await db.GetPollSeenCountAsync(third!.PollId), Is.Zero);
        Assert.That(await db.GetPollSeenCountAsync(first.PollId), Is.EqualTo(1));
        Assert.That(await db.GetPollSeenCountAsync(second.PollId), Is.EqualTo(1));
        await Server.WaitPost(() => SEntMan.System<SharedMindSystem>().WipeMind(mind));
    }

    private static IEnumerable<Control> Descendants(Control control)
    {
        foreach (var child in control.Children)
        {
            yield return child;
            foreach (var descendant in Descendants(child))
                yield return descendant;
        }
    }

    private async Task ReceiveList(Action request)
    {
        var received = new TaskCompletionSource();
        var polls = Client.ResolveDependency<ClientPollManager>();
        void OnList(List<PollData> data) => received.TrySetResult();
        await Client.WaitPost(() =>
        {
            polls.OnActivePollsUpdated += OnList;
            request();
        });
        try
        {
            await Pump(received.Task);
        }
        finally
        {
            await Client.WaitPost(() => polls.OnActivePollsUpdated -= OnList);
        }
    }

    private async Task RunServerTask(Func<Task> call)
    {
        Task task = null!;
        await Server.WaitPost(() => task = call());
        await Pump(task);
    }

    private async Task<T> RunServerTask<T>(Func<Task<T>> call)
    {
        Task<T> task = null!;
        await Server.WaitPost(() => task = call());
        await Pump(task);
        return await task;
    }

    private async Task Pump(Task task)
    {
        for (var i = 0; i < 100 && !task.IsCompleted; i++)
            await Pair.ReallyBeIdle(2);
        Assert.That(task.IsCompleted, Is.True, "The network/database operation did not complete.");
        await task;
    }

    private async Task WaitForView(int poll)
    {
        var db = Server.ResolveDependency<IServerDbManager>();
        for (var i = 0; i < 100; i++)
        {
            await Pair.ReallyBeIdle(2);
            var replicated = false;
            await Client.WaitPost(() => replicated = Client.ResolveDependency<ClientPollManager>().ActivePolls
                .TryGetValue(poll, out var data) && data.SeenCount == 1);
            if (replicated && await db.GetPollSeenCountAsync(poll) == 1)
                return;
        }
        Assert.Fail("The displayed poll was not marked seen.");
    }
}
