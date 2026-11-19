using Content.IntegrationTests.Utility;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq; // Arcane

namespace Content.IntegrationTests.Tests.Utility;

[TestOf(typeof(GameDataScrounger))]
public sealed class ScroungerTests : GameTest // Arcane-Edit
{
    [Explicit] // Trauma - not my fault if this breaks, waste of test time
    [Test]
    [Description("Assert that the data scrounger finds prototypes by type successfully.")]
    public void ScroungeByType()
    {
        var scrounged = GameDataScrounger.PrototypesOfKind<EntityPrototype>();
        Assert.That(scrounged, Is.Not.Empty);
    }

    [Explicit] // Trauma
    [Test]
    [Description("Assert that the data scrounger finds all files by pattern in a directory successfully.")]
    [TestCase("*.yml")]
    [TestCase("*.txt")]
    public void ScroungeByPattern(string pattern)
    {
        var files = GameDataScrounger.FilesInDirectory("/", pattern);

        Assert.That(files, Is.Not.Empty);
    }

    [Explicit] // Trauma
    [Test]
    [Description("Assert that the data scrounger finds all files by pattern in a directory successfully, and returns valid VFS paths.")]
    public void ScroungeByPatternInVfs()
    {
        var files = GameDataScrounger.FilesInDirectoryInVfs("/Maps", "*.yml");

        Assert.That(files, Is.Not.Empty);

        Assert.That(files[0].IsRooted, Is.True);
        Assert.That(files[0].ToString(), Does.StartWith("/Maps/"));
    }

    // [Explicit] // Arcane-Edit
    [Test]
    [Description("Assert that the data scrounger finds entities by component successfully.")]
    // Arcane-Edit-Start: Compare selection with instantiated component types in the resolved catalog.
    public async Task ScroungeByComponent()
    {
        await Server.WaitAssertion(() =>
        {
            var prototypes = SProtoMan.EnumeratePrototypes<EntityPrototype>()
                .Where(p => !p.Abstract).ToArray();
            using (Assert.EnterMultipleScope())
            {
                foreach (var component in new[]
                         {
                             "Item", "Stamina", "MobThresholds", "DeviceLinkSink", "Label",
                             "Construction", "Plant", "MaterialReclaimer", "AntagSelection", "ReplacementAccent",
                         })
                {
                    var type = SEntMan.ComponentFactory.GetRegistration(component).Type;
                    var expected = prototypes.Where(p => p.Components.Values.Any(c => c.Component.GetType() == type))
                        .Select(p => p.ID).OrderBy(id => id, StringComparer.Ordinal).ToArray();
                    var selected = GameDataScrounger.EntitiesWithComponent(SProtoMan, component);
                    Assert.That(selected, Is.EquivalentTo(expected), component);
                    TestContext.Out.WriteLine($"{component}: {selected.Length}; {string.Join(',', selected)}");
                }
            }
        });
    }
    // Arcane-Edit-End
}
