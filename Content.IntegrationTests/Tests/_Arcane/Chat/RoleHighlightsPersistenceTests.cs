// Arcane-Start
using Content.Arcane.Client.Chat.Highlights;
using Content.Client.UserInterface.Systems.Chat;
using Content.IntegrationTests.Fixtures;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Arcane.Chat;

[TestFixture]
public sealed class RoleHighlightsPersistenceTests : GameTest
{
    private static readonly ResPath Primary = new("/highlights.yml");
    private static readonly ResPath Backup = new("/highlights.backup.yml");
    private const string ValidBackup = "version: 1\nhighlights:\n  Captain: bridge\n  Botanist: garden\n";

    public override PoolSettings PoolSettings => new() { Connected = false, Fresh = true, Destructive = true };

    [Test]
    public async Task FailedBackupLoadRejectsSaveAndPreservesBothJobs()
    {
        var backup = ValidBackup.Replace("version: 1", "version: 999", StringComparison.Ordinal);
        var data = Client.ResolveDependency<IResourceManager>().UserData;
        data.WriteAllText(Backup, backup);
        Pair.ClientLogHandler.JudgeLog += (_, message) => message.RenderMessage().Contains("Failed to load role highlights", StringComparison.Ordinal);

        await Pair.Connect();
        await Client.WaitAssertion(() =>
        {
            var system = Client.EntMan.System<RoleHighlightsSystem>();
            Assert.That(system.SaveHighlights("Captain", "new bridge"), Is.EqualTo(RoleHighlightsSaveResult.Failed));
            Assert.That(data.Exists(Primary), Is.False);
            Assert.That(data.ReadAllText(Backup), Is.EqualTo(backup));
        });
    }

    [Test]
    public async Task CorruptPrimaryLoadsBackupWithoutDeletingIt()
    {
        var data = Client.ResolveDependency<IResourceManager>().UserData;
        data.WriteAllText(Primary, "invalid: [");
        data.WriteAllText(Backup, ValidBackup);
        Pair.ClientLogHandler.JudgeLog += (_, message) => message.RenderMessage().Contains("Failed to load role highlights", StringComparison.Ordinal);

        await Pair.Connect();
        await Client.WaitAssertion(() =>
        {
            var captain = new GetRoleHighlightsEvent("Captain");
            var botanist = new GetRoleHighlightsEvent("Botanist");
            Client.EntMan.EventBus.RaiseEvent(EventSource.Local, ref captain);
            Client.EntMan.EventBus.RaiseEvent(EventSource.Local, ref botanist);
            Assert.That(captain.Highlights, Is.EqualTo("bridge"));
            Assert.That(botanist.Highlights, Is.EqualTo("garden"));
            Assert.That(data.ReadAllText(Backup), Is.EqualTo(ValidBackup));

            var system = Client.EntMan.System<RoleHighlightsSystem>();
            Assert.That(system.SaveHighlights("Captain", "new bridge"), Is.EqualTo(RoleHighlightsSaveResult.Success));
            Assert.That(data.ReadAllText(Primary), Does.Contain("Botanist"));
            Assert.That(data.ReadAllText(Primary), Does.Contain("garden"));
        });
    }

    [Test]
    public void DefaultsUseCommonKeyAndCommaNormalization()
    {
        Assert.That(RoleHighlightDefaults.GetKey("Head Of Personnel"), Is.EqualTo("highlights-head-of-personnel"));
        Assert.That(RoleHighlightDefaults.Normalize("Captain,  \"Cap\",Bridge,, "), Is.EqualTo("Captain\n\"Cap\"\nBridge"));
    }
}
// Arcane-End
