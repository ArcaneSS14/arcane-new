using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.DetailExaminable;
using Content.Server.EUI;
using Content.Shared.DetailExaminable;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Arcane.Server.DetailExaminable;

/// <summary>
/// Replaces the detail examine tooltip with a window showing the full extended flavor text.
/// </summary>
public sealed partial class DetailExaminableSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private EuiManager _eui = default!;
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private InventorySystem _inventory = default!;

    private static readonly string[] CoveringSlots = ["jumpsuit", "outerClothing"];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DetailExaminableComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    private void OnGetExamineVerbs(Entity<DetailExaminableComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
            return;

        var detailsRange = _examine.IsInDetailsRange(args.User, ent);
        var user = args.User;

        args.Verbs.Add(new ExamineVerb
        {
            Act = () => OpenWindow(user, ent),
            Text = Loc.GetString("detail-examinable-verb-text"),
            Category = VerbCategory.Examine,
            Disabled = !detailsRange,
            Message = detailsRange ? null : Loc.GetString("detail-examinable-verb-disabled"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/examine.svg.192dpi.png")),
        });
    }

    private void OpenWindow(EntityUid user, Entity<DetailExaminableComponent> target)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        var showNsfw = _cfg.GetCVar(ArcaneCVars.FlavorNsfwEnabled)
                       && _cfg.GetCVar(ArcaneCVars.NsfwContentEnabled)
                       && !IsCovered(target);

        var profile = CompOrNull<HumanoidProfileComponent>(target);
        var state = new DetailExaminableEuiState(
            GetNetEntity(target),
            Identity.Name(target, EntityManager),
            profile?.Species ?? string.Empty,
            profile?.Sex ?? Sex.Unsexed,
            profile?.Gender ?? Robust.Shared.Enums.Gender.Epicene,
            target.Comp.Content,
            showNsfw ? target.Comp.Extended : target.Comp.Extended.WithoutNsfw(),
            showNsfw);

        var eui = new DetailExaminableEui(state);
        _eui.OpenEui(eui, actor.PlayerSession);
        eui.StateDirty();
    }

    private bool IsCovered(EntityUid uid)
    {
        foreach (var slot in CoveringSlots)
        {
            if (_inventory.TryGetSlotEntity(uid, slot, out _))
                return true;
        }

        return false;
    }
}
