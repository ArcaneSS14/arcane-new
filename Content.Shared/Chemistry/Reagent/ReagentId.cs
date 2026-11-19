using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.Chemistry.Reagent;

/// <summary>
/// Struct used to uniquely identify a reagent. This is usually just a ReagentPrototype id string, however some reagents
/// contain additional data (e.g., blood could store DNA data).
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public partial struct ReagentId : IEquatable<ReagentId>
{
    // TODO rename data field.
    [DataField("ReagentId", required: true)]
    public ProtoId<ReagentPrototype> Prototype { get; private set; }

    /// <summary>
    /// Any additional data that is unique to this reagent type. E.g., for blood this could be DNA data.
    /// </summary>
    [DataField]
    public List<ReagentData>? Data { get; private set; } = new();

    public ReagentId(string prototype, List<ReagentData>? data)
    {
        Prototype = prototype;
        // Goobstation start - fix shallow cloning of solution
        Data = new();
        if (data != null)
        {
            foreach (var reagentData in data)
            {
                Data.Add(reagentData.Clone());
            }
        }
        // Goobstation end
    }

    public ReagentId()
    {
        Prototype = default!;
        Data = new();
    }

    public List<ReagentData> EnsureReagentData()
    {
        return (Data != null) ? Data : new List<ReagentData>();
    }

    public bool Equals(ReagentId other)
    {
        if (Prototype != other.Prototype)
            return false;

        if (Data == null)
            return other.Data == null;

        if (other.Data == null)
            return false;

        // Arcane-Edit-Start
        if (ReferenceEquals(Data, other.Data))
            return true;

        if (Data.Count != other.Data.Count)
            return false;

        if (Data.Count == 0)
            return true;

        if (Data.Count == 1)
            return other.Data.Contains(Data[0]) && Data.Contains(other.Data[0]);

        // Keep the existing set comparison for larger metadata lists; repeated Contains becomes quadratic.
        if (Data.Except(other.Data).Any() || other.Data.Except(Data).Any())
            return false;
        // Arcane-Edit-End

        return true;
    }

    public bool Equals(string prototype, List<ReagentData>? otherData = null)
    {
        if (Prototype != prototype)
            return false;

        if (Data == null)
            return otherData == null;

        return Data.Equals(otherData);
    }

    public override bool Equals(object? obj)
    {
        return obj is ReagentId other && Equals(other);
    }

    public override int GetHashCode()
    {
        // Arcane-Start: Equal metadata has the same count and members, in any order.
        if (Data is not { Count: > 0 } data)
            return HashCode.Combine(Prototype, Data?.Count ?? -1);

        if (data.Count == 1)
            return HashCode.Combine(Prototype, data[0].GetHashCode());

        // Extrema are independent of both order and duplicate multiplicities.
        var lowest = int.MaxValue;
        var highest = int.MinValue;
        foreach (var entry in data)
        {
            var hash = entry.GetHashCode();
            lowest = Math.Min(lowest, hash);
            highest = Math.Max(highest, hash);
        }

        return HashCode.Combine(Prototype, data.Count, lowest, highest);
        // Arcane-End
    }

    public string ToString(FixedPoint2 quantity)
    {
        return $"{Prototype}:{quantity}";
    }

    public override string ToString()
    {
        return $"{Prototype}";
    }

    public static bool operator ==(ReagentId left, ReagentId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ReagentId left, ReagentId right)
    {
        return !(left == right);
    }
}
