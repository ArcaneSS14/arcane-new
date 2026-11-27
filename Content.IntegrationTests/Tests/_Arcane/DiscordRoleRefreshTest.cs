// Arcane-Start: role refreshes must not restore stale permissions
#nullable enable
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;
using Content.Arcane.Server.DiscordRoles;
using Content.Arcane.Shared.DiscordRoles;
using Content.IntegrationTests.Fixtures;
using Content.Server.Database;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class DiscordRoleRefreshTest : GameTest
{
    [Test]
    public async Task RefreshOrderingAndDisconnectDoNotRestoreOldRoles()
    {
        var manager = Server.ResolveDependency<DiscordRoleManager>();
        var field = typeof(DiscordRoleManager).GetField("_db", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var originalDb = (IServerDbManager) field.GetValue(manager)!;
        var db = DispatchProxy.Create<IServerDbManager, DiscordRoleDbProbe>();
        var probe = (DiscordRoleDbProbe) db;
        probe.Inner = originalDb;
        var role = DiscordRole.SponsorTier2;
        var roleId = DiscordRoleIds.All[role];
        Task first = Task.CompletedTask;
        Task second = Task.CompletedTask;

        async Task PumpUntil(Func<bool> condition)
        {
            for (var i = 0; i < 500 && !condition(); i++)
            {
                await Server.WaitRunTicks(1);
                await Task.Delay(10);
            }
            Assert.That(condition(), Is.True, "The role request did not reach the expected state.");
        }

        var oldResponse = probe.AddResponse();
        var newResponse = probe.AddResponse();
        try
        {
            await Server.WaitPost(() =>
            {
                field.SetValue(manager, db);
                first = manager.ReloadRoles(ServerSession!);
                second = manager.ReloadRoles(ServerSession!);
            });
            Assert.That(probe.Calls, Is.EqualTo(1), "The second query must wait for the first for the same user.");
            oldResponse.SetResult([roleId]);
            await PumpUntil(() => probe.Calls == 2);
            newResponse.SetResult([]);
            await PumpUntil(() => first.IsCompleted && second.IsCompleted);
            await Task.WhenAll(first, second);
            await Server.WaitAssertion(() => Assert.That(manager.HasRole(ServerSession!, role), Is.False));

            var delayedOldSession = probe.AddResponse();
            var freshSession = probe.AddResponse();
            await Server.WaitPost(() =>
            {
                first = manager.ReloadRoles(ServerSession!);
                typeof(DiscordRoleManager).GetMethod("ClientDisconnected", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(manager, [ServerSession!]);
                second = manager.ReloadRoles(ServerSession!);
            });
            Assert.That(probe.Calls, Is.EqualTo(4), "A new session must not wait on the disconnected session.");
            freshSession.SetResult([]);
            await PumpUntil(() => second.IsCompleted);
            delayedOldSession.SetResult([roleId]);
            await PumpUntil(() => first.IsCompleted);
            await Task.WhenAll(first, second);
            await Server.WaitAssertion(() => Assert.That(manager.HasRole(ServerSession!, role), Is.False));
        }
        finally
        {
            await Server.WaitPost(() => field.SetValue(manager, originalDb));
        }
    }
}

[Virtual]
public class DiscordRoleDbProbe : DispatchProxy
{
    public IServerDbManager Inner = default!;
    private readonly ConcurrentQueue<TaskCompletionSource<List<ulong>>> _responses = new();
    private int _calls;
    public int Calls => Volatile.Read(ref _calls);

    public TaskCompletionSource<List<ulong>> AddResponse()
    {
        var response = new TaskCompletionSource<List<ulong>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _responses.Enqueue(response);
        return response;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod!.Name == nameof(IServerDbManager.GetDiscordRoleIds))
        {
            Interlocked.Increment(ref _calls);
            return _responses.TryDequeue(out var response)
                ? response.Task
                : throw new InvalidOperationException("Unexpected Discord role query.");
        }
        return targetMethod.Invoke(Inner, args);
    }
}
// Arcane-End
