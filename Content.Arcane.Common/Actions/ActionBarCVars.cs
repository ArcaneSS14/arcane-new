using Robust.Shared.Configuration;

namespace Content.Arcane.Common.Actions;

[CVarDefs]
public static class ActionBarCVars
{
    public static readonly CVarDef<bool> LockDrag =
        CVarDef.Create("ui.lock_action_bar_drag", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> FreePlacement =
        CVarDef.Create("hud.actions_bar_free_placement", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> PositionX =
        CVarDef.Create("hud.actions_bar_position_x", -1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> PositionY =
        CVarDef.Create("hud.actions_bar_position_y", -1f, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<string> SlotPositions =
        CVarDef.Create("hud.actions_bar_slot_positions", string.Empty, CVar.CLIENTONLY | CVar.ARCHIVE);
}
