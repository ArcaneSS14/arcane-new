// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Arcane.Shared.Stains;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Clothing.EntitySystems;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class ChameleonStainVisualTest : InteractionTest
{
    [Test]
    public async Task ChangingDisguiseKeepsStainAndDisguiseLayers()
    {
        const string disguise = "ClothingUniformJumpsuitColorRed";
        var netSuit = await SpawnTarget("ClothingUniformJumpsuitChameleon");
        var suit = ToServer(netSuit);

        await Server.WaitPost(() =>
        {
            var solutions = SEntMan.System<SharedSolutionContainerSystem>();
            Assert.That(solutions.TryGetSolution(suit, "stain", out var solution), Is.True);
            Assert.That(solutions.TryAddReagent(solution!.Value, "Blood", 1), Is.True);
        });
        await RunUntilSynced();
        var clientSuit = ToClient(netSuit);
        await Client.WaitPost(() =>
        {
            var appearance = CEntMan.GetComponent<AppearanceComponent>(clientSuit);
            CEntMan.System<SharedAppearanceSystem>().QueueUpdate(clientSuit, appearance);
        });
        await RunTicks(5);
        await Client.WaitAssertion(() =>
            Assert.That(CEntMan.GetComponent<StainableComponent>(clientSuit).RevealedIconVisuals, Has.Count.EqualTo(1)));

        await Server.WaitPost(() => SEntMan.System<SharedChameleonClothingSystem>()
            .SetSelectedPrototype(suit, disguise, validate: false));
        await RunUntilSynced();

        void AssertLayers()
        {
            var proto = CProtoMan.Index<EntityPrototype>(disguise);
            Assert.That(proto.TryComp(out SpriteComponent? sourceSprite, CEntMan.ComponentFactory), Is.True);
            var sprite = CEntMan.GetComponent<SpriteComponent>(clientSuit);
            var stain = CEntMan.GetComponent<StainableComponent>(clientSuit);
            Assert.That(stain.RevealedIconVisuals, Has.Count.EqualTo(1));
            Assert.That(sprite.AllLayers.Count(), Is.EqualTo(sourceSprite!.AllLayers.Count() + 1));
        }

        await Client.WaitAssertion(AssertLayers);
        await Client.WaitPost(() =>
        {
            var appearance = CEntMan.GetComponent<AppearanceComponent>(clientSuit);
            CEntMan.System<SharedAppearanceSystem>().QueueUpdate(clientSuit, appearance);
        });
        await RunTicks(5);
        await Client.WaitAssertion(AssertLayers);
    }
}
