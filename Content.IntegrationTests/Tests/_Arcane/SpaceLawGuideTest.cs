// Arcane-Start: the Space Law root must lead to the ported Arcane guide
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Guidebook;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

[TestFixture]
public sealed class SpaceLawGuideTest : GameTest
{
    private static readonly ProtoId<GuideEntryPrototype> SpaceLaw = "SpaceLaw";

    [Test]
    public async Task RootUsesArcaneTextAndSections()
    {
        await Server.WaitAssertion(() =>
        {
            var prototypes = Server.ResolveDependency<IPrototypeManager>();
            var root = prototypes.Index(SpaceLaw);
            Assert.That(root.Text.ToString(), Is.EqualTo("/ServerInfo/_Arcane/Guidebook/SpaceLaw/SpaceLawIntro.xml"));
            var expected = new[] { "SpaceLawTable", "Procedures", "SpaceRights", "PunishmentTypes", "PunishmentTerms", "UseOfForce" };
            Assert.That(root.Children.Select(child => child.Id), Is.EquivalentTo(expected));
            foreach (var child in root.Children)
                Assert.That(prototypes.HasIndex(child), Is.True);
        });
    }
}
// Arcane-End
