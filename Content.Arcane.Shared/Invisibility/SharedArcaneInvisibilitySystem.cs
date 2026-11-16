using Content.Shared.Body;
using Content.Shared.Humanoid;

namespace Content.Arcane.Shared.Invisibility;

public sealed partial class SharedArcaneInvisibilitySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ArcaneInvisibilityOrganComponent, OrganGotInsertedEvent>(OnOrganInserted);
        SubscribeLocalEvent<ArcaneInvisibilityOrganComponent, OrganGotRemovedEvent>(OnOrganRemoved);
    }

    private void OnOrganInserted(Entity<ArcaneInvisibilityOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        EnsureComp<ArcaneInvisibilityComponent>(args.Target);
    }

    private void OnOrganRemoved(Entity<ArcaneInvisibilityOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        RemComp<ArcaneInvisibilityComponent>(args.Target);
    }

    /// <summary>
    ///     Humanoid body visual layers (skin and markings, never clothing) that the arcane
    ///     invisibility shader applies to.
    /// </summary>
    public static readonly HumanoidVisualLayers[] BodyLayers =
    [
        HumanoidVisualLayers.Special,
        HumanoidVisualLayers.Tail,
        HumanoidVisualLayers.TailOverlay,
        HumanoidVisualLayers.Wings,
        HumanoidVisualLayers.Hair,
        HumanoidVisualLayers.FacialHair,
        HumanoidVisualLayers.UndergarmentTop,
        HumanoidVisualLayers.UndergarmentBottom,
        HumanoidVisualLayers.Face,
        HumanoidVisualLayers.Chest,
        HumanoidVisualLayers.Head,
        HumanoidVisualLayers.Snout,
        HumanoidVisualLayers.SnoutCover,
        HumanoidVisualLayers.HeadSide,
        HumanoidVisualLayers.HeadTop,
        HumanoidVisualLayers.Eyes,
        HumanoidVisualLayers.RArm,
        HumanoidVisualLayers.LArm,
        HumanoidVisualLayers.RHand,
        HumanoidVisualLayers.LHand,
        HumanoidVisualLayers.RLeg,
        HumanoidVisualLayers.LLeg,
        HumanoidVisualLayers.RFoot,
        HumanoidVisualLayers.LFoot,
    ];
}
