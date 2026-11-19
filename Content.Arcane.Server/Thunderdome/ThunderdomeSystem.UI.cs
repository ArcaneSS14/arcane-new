using System.Linq;
using Content.Arcane.Common.Thunderdome;
using Content.Arcane.Shared.Thunderdome;
using Content.Server.EUI;
using Content.Shared.Eui;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Arcane.Server.Thunderdome;

public sealed partial class ThunderdomeSystem
{
    [Dependency] private EuiManager _eui = default!;
    [Dependency] private IConfigurationManager _configuration = default!;

    private readonly Dictionary<NetUserId, ThunderdomeLoadoutEui> _loadoutMenus = new();

    private void InitializeUi()
    {
        SubscribeNetworkEvent<ThunderdomeJoinEvent>(OnJoinRequested);
    }

    private void OnJoinRequested(ThunderdomeJoinEvent message, EntitySessionEventArgs args)
    {
        var player = args.SenderSession;
        if (!_configuration.GetCVar(ThunderdomeCVars.Enabled) || _loadoutMenus.ContainsKey(player.UserId) ||
            !CanJoin(player, out _) || !TryGetArena(out var arena))
            return;

        var options = arena.Comp.WeaponLoadouts.Select(loadout => new ThunderdomeLoadoutOption(
            Loc.GetString(loadout.Name), Loc.GetString(loadout.Description), Loc.GetString(loadout.Category), loadout.Sprite)).ToArray();
        var menu = new ThunderdomeLoadoutEui(this, arena.Owner, options);
        _loadoutMenus.Add(player.UserId, menu);
        _eui.OpenEui(menu, player);
    }

    internal void OnMenuClosed(ThunderdomeLoadoutEui menu)
    {
        if (_loadoutMenus.TryGetValue(menu.Player.UserId, out var current) && current == menu)
            _loadoutMenus.Remove(menu.Player.UserId);
    }

    private void CloseLoadoutMenus()
    {
        foreach (var menu in _loadoutMenus.Values.ToArray())
            menu.Close();
    }
}

public sealed class ThunderdomeLoadoutEui(ThunderdomeSystem arena, EntityUid rule, ThunderdomeLoadoutOption[] options) : BaseEui
{
    public override void Opened() => StateDirty();

    public override EuiStateBase GetNewState() => new ThunderdomeLoadoutState(options);

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);
        if (msg is ThunderdomeSelectLoadoutMessage selection && arena.TryJoin(Player, rule, selection.Index))
            Close();
    }

    public override void Closed() => arena.OnMenuClosed(this);
}
