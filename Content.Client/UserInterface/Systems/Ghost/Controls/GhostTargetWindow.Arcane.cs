// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Arcane.Common.Ghost;
using Content.Shared.Roles;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Ghost.Controls;

/// <summary>
/// Arcane - groups the warp list by antagonists, departments and player state (Orion #107, #121).
/// Without details from the server the vanilla flat list is shown.
/// </summary>
public sealed partial class GhostTargetWindow
{
    private HashSet<NetEntity> _warpPoints = new();
    private Dictionary<NetEntity, GhostWarpPlayerDetail>? _players;
    private Dictionary<NetEntity, GhostWarpAntagonist>? _antagonists;

    public void SetDetails(GhostWarpDetailsEvent details)
    {
        _players = details.Players.ToDictionary(p => p.Entity);
        _antagonists = details.Antagonists.ToDictionary(a => a.Entity);
    }

    private void AddGroupedButtons()
    {
        var proto = IoCManager.Resolve<IPrototypeManager>();
        var sections = new List<(string Title, Color? Color, int Order, List<Button> Buttons)>();
        var byKey = new Dictionary<string, List<Button>>();

        List<Button> Section(string key, string title, Color? color, int order)
        {
            if (byKey.TryGetValue(key, out var list))
                return list;

            list = new List<Button>();
            byKey[key] = list;
            sections.Add((title, color, order, list));
            return list;
        }

        foreach (var (name, target) in _warps)
        {
            var button = CreateWarpButton(name, target);

            if (_warpPoints.Contains(target))
            {
                Section("places", Loc.GetString("ghost-warp-section-places"), null, 0).Add(button);
            }
            else if (_antagonists!.TryGetValue(target, out var antag))
            {
                button.Text = $"{name} ({antag.AntagonistName})";
                button.ToolTip = antag.Description;
                Section("antagonists", Loc.GetString("ghost-warp-section-antagonists"), Color.Red, 1).Add(button);
            }
            else if (_players!.TryGetValue(target, out var player))
            {
                button.Text = $"{name} ({player.JobName})";
                switch (player.State)
                {
                    case GhostWarpPlayerState.Dead:
                        Section("dead", Loc.GetString("ghost-warp-section-dead"), Color.Gray, 1000).Add(button);
                        break;
                    case GhostWarpPlayerState.Left:
                        Section("left", Loc.GetString("ghost-warp-section-left"), Color.DarkGray, 1001).Add(button);
                        break;
                    case GhostWarpPlayerState.Ghost:
                        Section("ghosts", Loc.GetString("ghost-warp-section-ghosts"), Color.LightGray, 1002).Add(button);
                        break;
                    default:
                        if (player.DepartmentId != null && proto.TryIndex<DepartmentPrototype>(player.DepartmentId, out var dept))
                            Section(dept.ID, Loc.GetString(dept.Name), dept.Color, 10 + dept.Weight).Add(button);
                        else
                            Section("other", Loc.GetString("ghost-warp-section-other"), null, 999).Add(button);
                        break;
                }
            }
            else
            {
                Section("other", Loc.GetString("ghost-warp-section-other"), null, 999).Add(button);
            }
        }

        foreach (var (title, color, _, buttons) in sections.OrderBy(s => s.Order))
        {
            var box = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 5,
            };

            box.AddChild(new Label
            {
                Text = title,
                StyleClasses = { "LabelBig" },
                FontColorOverride = color,
                HorizontalAlignment = HAlignment.Center,
            });

            foreach (var button in buttons)
            {
                if (color is { } c)
                    button.ModulateSelfOverride = Color.InterpolateBetween(Color.White, c, 0.35f);

                box.AddChild(button);
            }

            ButtonContainer.AddChild(box);
        }

        UpdateVisibleButtons();
    }

    private Button CreateWarpButton(string name, NetEntity target)
    {
        var button = new Button
        {
            Text = name,
            TextAlign = Label.AlignMode.Right,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            SizeFlagsStretchRatio = 1,
            MinSize = new Vector2(340, 20),
            ClipText = true,
        };

        button.OnPressed += _ => WarpClicked?.Invoke(target);
        return button;
    }

    /// <summary>
    /// Hides search misses inside sections, and sections left without visible buttons.
    /// </summary>
    private void UpdateGroupedVisibility()
    {
        foreach (var child in ButtonContainer.Children)
        {
            if (child is not BoxContainer box)
                continue;

            var any = false;
            foreach (var entry in box.Children)
            {
                if (entry is not Button button)
                    continue;

                button.Visible = ButtonIsVisible(button);
                any |= button.Visible;
            }

            box.Visible = any;
        }
    }
}
