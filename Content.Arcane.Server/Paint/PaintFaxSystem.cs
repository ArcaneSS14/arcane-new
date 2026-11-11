using Content.Arcane.Shared.Paint;
using Content.Shared.Fax;
using Content.Shared.Popups;

namespace Content.Arcane.Server.Paint;

public sealed partial class PaintFaxSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PaintSurfaceComponent, FaxOperationAttemptEvent>(OnFax);
    }

    private void OnFax(Entity<PaintSurfaceComponent> ent, ref FaxOperationAttemptEvent args)
    {
        args.Handled = true;
        if (args.Actor.IsValid())
            _popup.PopupEntity(Loc.GetString("paint-fax-use-photocopier"), args.Fax, args.Actor);
    }
}
