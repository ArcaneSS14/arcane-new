using Content.Arcane.Server.Voodoo;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Forensics.Components;
using Content.Shared.Interaction;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Arcane;

public sealed class P26VoodooRuntimeTest : InteractionTest
{
    private EntityCoordinates Center => MapData.GridCoords.Offset(new Vector2(0.5f, 0.5f));

    [Test]
    public async Task VoodooConsumesOneUnitOnlyForAnUnambiguousBodyAndClearsOnDeletion()
    {
        await Server.WaitAssertion(() =>
        {
            var user = SEntMan.SpawnAttachedTo("MobHuman", Center);
            var body = SEntMan.SpawnAttachedTo("MobHuman", Center);
            var duplicate = SEntMan.SpawnAttachedTo("MobHuman", Center);
            var doll = SEntMan.SpawnAttachedTo("ArcaneVoodooBlood", Center);
            var beaker = SEntMan.SpawnAttachedTo("Beaker", Center);
            var link = SEntMan.GetComponent<VoodooComponent>(doll);
            var chemistry = SEntMan.System<SharedSolutionContainerSystem>();
            var dna = SEntMan.GetComponent<DnaComponent>(body).DNA;
            Assert.That(dna, Is.Not.Null.And.Not.Empty);
            var duplicateDna = SEntMan.GetComponent<DnaComponent>(duplicate);
            var originalDna = duplicateDna.DNA;
            duplicateDna.DNA = dna;
            var blood = new ReagentId("Blood", [new DnaData { DNA = dna! }]);
            Assert.That(chemistry.TryGetSolution(beaker, "beaker", out var solutionEntity, out var solution), Is.True);
            Assert.That(chemistry.TryAddReagent(solutionEntity!.Value, blood, 2, out _), Is.True);

            Bind();
            Assert.That(link.Target, Is.Null);
            Assert.That(solution!.GetTotalPrototypeQuantity("Blood"), Is.EqualTo(FixedPoint2.New(2)));

            duplicateDna.DNA = originalDna;
            Bind();
            Assert.That(link.Target, Is.EqualTo(body));
            Assert.That(solution.GetTotalPrototypeQuantity("Blood"), Is.EqualTo(FixedPoint2.New(1)));

            SEntMan.DeleteEntity(body);
            Assert.That(link.Target, Is.Null);

            void Bind()
            {
                var ev = new InteractUsingEvent(user, beaker, doll, Center);
                SEntMan.EventBus.RaiseLocalEvent(doll, ev);
                Assert.That(ev.Handled, Is.True);
            }
        });
    }
}
