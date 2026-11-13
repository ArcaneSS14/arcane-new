// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Goobstation.Shared.SetSelector;
using Robust.Shared.Utility; // Arcane

namespace Content.Goobstation.Client.SetSelector;

[GenerateTypedNameReferences]
public sealed partial class SelectableSet : Control
{
    public SelectableSet(SelectableSetInfo set, SpriteSystem spriteSystem)
    {
        RobustXamlLoader.Load(this);

        Icon.Texture = spriteSystem.Frame0(set.Sprite);
        SetName.Text = Loc.GetString(set.Name);
        SetDescription.SetMessage(FormattedMessage.FromUnformatted(Loc.GetString(set.Description))); // Arcane-Edit: Label text > wrapping plain-text description
        SetButton.Text = Loc.GetString(set.Selected ? "set-selector-button-deselect" : "set-selector-button-select");
        SetButton.ModulateSelfOverride = set.Selected ? new Color(40, 84, 35) : new Color(68, 75, 103);
    }
}
