using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Utility;
using Content.Shared.Labels.Components;

namespace Content.IntegrationTests.Tests.Labels;

public sealed class LabelPrototypeTest : GameTest
{
    // private static string[] _entitiesWithLabel = GameDataScrounger.EntitiesWithComponent("Label"); // Arcane-Edit

    // TODO: inline this slop bruh
    [Test]
    [TestOf(typeof(LabelComponent))]
    // [TestCaseSource(nameof(_entitiesWithLabel))] // Arcane-Edit
    [Description("Ensures entity prototypes do not set LabelComponent.CurrentLabel directly.")]
    public async Task CurrentLabelNotSetInPrototype() // Arcane-Edit
    {
        var pair = Pair;
        var server = pair.Server;
        var protoMan = server.ProtoMan;

        // Arcane-Edit-Start
        await server.WaitAssertion(() =>
        {
            using var assertions = Assert.EnterMultipleScope();
            foreach (var protoKey in GameDataScrounger.EntitiesWithComponent(protoMan, "Label"))
            {
                var proto = protoMan.Index(protoKey);
                var comp = (LabelComponent)proto.Components["Label"].Component;

                Assert.That(comp.CurrentLabel, Is.Null.Or.Empty,
                    $"Prototype {proto.ID} sets LabelComponent.CurrentLabel directly. Use LabelComponent.LocalizedLabel instead.");
            }
        });
        // Arcane-Edit-End
    }
}
