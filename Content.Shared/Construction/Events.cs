using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

// Arcane-Start
using Content.Shared.FixedPoint;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Arcane.Common.Construction.Prototypes;
// Arcane-End

namespace Content.Shared.Construction;

/// <summary>
///     Sent client -> server to to tell the server that we started building
///     a structure-construction.
/// </summary>
[Serializable, NetSerializable]
public sealed class TryStartStructureConstructionMessage : EntityEventArgs
{
    /// <summary>
    ///     Position to start building.
    /// </summary>
    public readonly NetCoordinates Location;

    /// <summary>
    ///     The construction prototype to start building.
    /// </summary>
    public readonly string PrototypeName;

    public readonly Angle Angle;

    /// <summary>
    ///     Identifier to be sent back in the acknowledgement so that the client can clean up its ghost.
    /// </summary>
    /// <remarks>
    /// So essentially the client is sending its own entity to the server so it knows to delete it when it gets server
    /// response back.
    /// </remarks>
    public readonly int Ack;

    // Goobstation
    public readonly NetEntity? With = null;

    public TryStartStructureConstructionMessage(NetCoordinates loc, string prototypeName, Angle angle, int ack, NetEntity? with = null) // Goobstation
    {
        Location = loc;
        PrototypeName = prototypeName;
        Angle = angle;
        Ack = ack;
        With = with; // Goobstation
    }
}

/// <summary>
///     Sent client -> server to to tell the server that we started building
///     an item-construction.
/// </summary>
[Serializable, NetSerializable]
public sealed class TryStartItemConstructionMessage : EntityEventArgs
{
    /// <summary>
    ///     The construction prototype to start building.
    /// </summary>
    public readonly string PrototypeName;

    public TryStartItemConstructionMessage(string prototypeName)
    {
        PrototypeName = prototypeName;
    }
}

/// <summary>
/// Sent server -> client to tell the client that a ghost has started to be constructed.
/// </summary>
[Serializable, NetSerializable]
public sealed class AckStructureConstructionMessage : EntityEventArgs
{
    public readonly int GhostId;

    /// <summary>
    ///     The entity that is now being constructed, if any.
    /// </summary>
    public readonly NetEntity? Uid;

    public AckStructureConstructionMessage(int ghostId, NetEntity? uid = null)
    {
        GhostId = ghostId;
        Uid = uid;
    }
}

/// <summary>
/// Sent client -> server to request a specific construction guide.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestConstructionGuide : EntityEventArgs
{
    public readonly string ConstructionId;

    public RequestConstructionGuide(string constructionId)
    {
        ConstructionId = constructionId;
    }
}

/// <summary>
/// Sent server -> client as a response to a <see cref="RequestConstructionGuide"/> net message.
/// </summary>
[Serializable, NetSerializable]
public sealed class ResponseConstructionGuide : EntityEventArgs
{
    public readonly string ConstructionId;
    public readonly ConstructionGuide Guide;

    public ResponseConstructionGuide(string constructionId, ConstructionGuide guide)
    {
        ConstructionId = constructionId;
        Guide = guide;
    }
}

[Serializable, NetSerializable]
public sealed partial class ConstructionInteractDoAfterEvent : DoAfterEvent
{
    [DataField("clickLocation")]
    public NetCoordinates ClickLocation;

    private ConstructionInteractDoAfterEvent()
    {
    }

    public ConstructionInteractDoAfterEvent(IEntityManager entManager, InteractUsingEvent ev)
    {
        ClickLocation = entManager.GetNetCoordinates(ev.ClickLocation);
    }

    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class ToolRefineDoAfterEvent : SimpleDoAfterEvent
{
}

// Arcane-Start
public struct MachinePartState
{
    public MachinePartComponent Part;
    public StackComponent? Stack;

    public readonly int Quantity()
    {
        return Stack?.Count ?? 1;
    }
}

public sealed class RefreshPartsEvent : EntityEventArgs
{
    public IReadOnlyList<MachinePartState> Parts = new List<MachinePartState>();
    public Dictionary<ProtoId<MachinePartPrototype>, float> PartRatings = new();

    public float GetPartRating(ProtoId<MachinePartPrototype> partId, float defaultValue = 1f)
    {
        return PartRatings.GetValueOrDefault(partId, defaultValue);
    }

    public float GetPartRatingSum(ProtoId<MachinePartPrototype> partId)
    {
        var sum = 0f;
        foreach (var state in Parts)
        {
            if (state.Part.Part != partId)
                continue;

            sum += state.Part.Tier * state.Quantity();
        }

        return sum;
    }

    public static float GetPartCooldownMultiplier(float tier)
    {
        return Math.Clamp(1.15f - tier * 0.15f, 0.1f, 1f);
    }

    public static float GetLinearMultiplier(float tier, float step, float min, float max)
    {
        return Math.Clamp(1.2f - tier * step, min, max);
    }

    public static float GetPositiveTierMultiplier(float tier, float @base = 1f)
    {
        return Math.Max(@base, tier);
    }
}

public sealed class UpgradeExamineEvent : EntityEventArgs
{
    private readonly FormattedMessage _message;

    public UpgradeExamineEvent(ref FormattedMessage message)
    {
        _message = message;
    }

    public void AddPercentageUpgrade(string upgradedLocId, float multiplier)
    {
        var percent = Math.Round(100 * MathF.Abs(multiplier - 1), 2);
        var locId = multiplier switch
        {
            < 1 => "machine-upgrade-decreased-by-percentage",
            1 or float.NaN => "machine-upgrade-not-upgraded",
            > 1 => "machine-upgrade-increased-by-percentage",
        };

        _message.TryAddMarkup(Loc.GetString(locId,
            ("upgraded", Loc.GetString(upgradedLocId)),
            ("percent", percent)) + '\n',
            out _);
    }

    public void AddPercentageUpgrade(string upgradedLocId, float multiplier, float timeModifier)
    {
        var locId = multiplier switch
        {
            < 1 => "machine-upgrade-decreased-by-percentage-extra",
            1 or float.NaN => "machine-upgrade-not-upgraded-extra",
            > 1 => "machine-upgrade-increased-by-percentage-extra",
        };

        var percentValue = 0f;

        if (float.IsFinite(multiplier) && float.IsFinite(timeModifier) && timeModifier > 0f)
        {
            percentValue = multiplier switch
            {
                < 1 => 100f * timeModifier * MathF.Abs(multiplier - 1f),
                > 1 => 100f / timeModifier * MathF.Abs(multiplier - 1f),
                _ => 100f / timeModifier,
            };

            if (!float.IsFinite(percentValue))
                percentValue = 0f;
        }

        FixedPoint2 percent = percentValue;

        var color = timeModifier switch
        {
            < 1 => "#6DFFA5",
            1 or float.NaN => "#FFFFFF",
            > 1 => "#FF7A7A",
        };

        _message.TryAddMarkup(Loc.GetString(locId,
            ("upgraded", Loc.GetString(upgradedLocId)),
            ("percent", percent),
            ("color", color)) + '\n',
            out _);
    }
}
// Arcane-End
