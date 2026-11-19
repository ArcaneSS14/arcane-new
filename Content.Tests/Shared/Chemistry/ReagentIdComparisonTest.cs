#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Chemistry.Reagent;
using NUnit.Framework;

namespace Content.Tests.Shared.Chemistry;

public sealed class ReagentIdComparisonTest
{
    [Test]
    public void MetadataComparisonPreservesExistingMembershipAndCountRules()
    {
        var empty = Id("Blood");
        var single = Id("Blood", "a");
        var sameReference = single;
        var clone = Id("Blood", "a");
        var fresh = new ReagentId("Blood", [new DnaData { DNA = "a", Freshness = TimeSpan.FromMinutes(3) }]);

        Assert.Multiple(() =>
        {
            Assert.That(default(ReagentId).Equals(default(ReagentId)), Is.True);
            Assert.That(default(ReagentId).Equals(new ReagentId()), Is.False, "Null and empty metadata remain distinct.");
            Assert.That(empty.Equals(Id("Blood")), Is.True);
            Assert.That(empty.Equals(single), Is.False);
            Assert.That(single.Equals(Id("Water", "a")), Is.False);
            Assert.That(single.Equals(sameReference), Is.True);
            Assert.That(single.Equals(clone), Is.True);
            Assert.That(single.GetHashCode(), Is.EqualTo(clone.GetHashCode()));
            Assert.That(single.Equals(fresh), Is.True, "DNA freshness intentionally does not distinguish blood.");
            Assert.That(single.Equals(Id("Blood", "b")), Is.False);
            Assert.That(Id("Blood", "a", "b").Equals(Id("Blood", "b", "a")), Is.True);
            Assert.That(Id("Blood", "a", "b").Equals(Id("Blood", "a", "c")), Is.False);
            Assert.That(Id("Blood", "a").Equals(Id("Blood", "a", "a")), Is.False);
            Assert.That(Id("Blood", "a", "a").Equals(Id("Blood", "a", "b")), Is.False);
            Assert.That(Id("Blood", "a", "b").Equals(Id("Blood", "a", "a")), Is.False);
            Assert.That(Id("Blood", "a", "a", "b").Equals(Id("Blood", "a", "b", "b")), Is.True,
                "Existing equality compares total count and membership, not duplicate multiplicities.");
        });
    }

    [Test]
    public void HashedLookupPreservesMetadataMembership()
    {
        var mixed = Id("Blood", "a", "b");
        var repeated = Id("Blood", "a", "a", "b");
        var values = new Dictionary<ReagentId, int>
        {
            [mixed] = 1,
            [repeated] = 2,
        };

        Assert.Multiple(() =>
        {
            Assert.That(values.TryGetValue(Id("Blood", "b", "a"), out var reversed), Is.True);
            Assert.That(reversed, Is.EqualTo(1));
            Assert.That(values.TryGetValue(Id("Blood", "a", "b", "b"), out var replacedDuplicate), Is.True);
            Assert.That(replacedDuplicate, Is.EqualTo(2));
            Assert.That(values.ContainsKey(Id("Blood", "a", "c")), Is.False);
        });
    }

    private static ReagentId Id(string prototype, params string[] dna)
        => new(prototype, dna.Select(value => (ReagentData) new DnaData { DNA = value }).ToList());
}
