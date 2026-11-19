using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Moq;
using NUnit.Framework;
using Robust.Shared.Random;

namespace Content.Tests.Shared._Arcane;

[TestFixture]
public sealed class WeightedPickBoundaryTest
{
    [Test]
    public void FractionalWeightsCoverTheLastRepresentableDraw()
    {
        var random = new Mock<IRobustRandom>();
        random.Setup(r => r.NextFloat()).Returns(MathF.BitDecrement(1f));
        var weights = Enumerable.Range(0, 11).ToDictionary(i => i, _ => 0.01f);
        var prototype = new Mock<IWeightedRandomPrototype>();
        prototype.SetupGet(p => p.Weights)
            .Returns(Enumerable.Range(0, 11).ToDictionary(i => $"entry-{i}", _ => 0.01f));

        Assert.Multiple(() =>
        {
            Assert.That(() => random.Object.Pick(weights), Is.EqualTo(10));
            Assert.That(() => SharedRandomExtensions.Pick(weights, random.Object), Is.EqualTo(10));
            Assert.That(() => SharedRandomExtensions.Pick(prototype.Object, random.Object), Is.EqualTo("entry-10"));
        });

        Assert.Throws<InvalidOperationException>(() => random.Object.Pick(new Dictionary<int, float>()));
        Assert.Throws<InvalidOperationException>(() => random.Object.Pick(new Dictionary<int, float> { [0] = float.NaN }));
    }
}
