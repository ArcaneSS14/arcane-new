// SPDX-License-Identifier: AGPL-3.0-or-later


namespace Content.Trauma.Client.Knowledge.UI;

[GenerateTypedNameReferences]
public sealed partial class SkillCategory : BoxContainer
{
    public SkillCategory(string name, Color color) // Arcane-Edit: name > name and color
    {
        RobustXamlLoader.Load(this);

        SkillLabel.Text = name;
        SkillLabel.FontColorOverride = color; // Arcane
    }
}
