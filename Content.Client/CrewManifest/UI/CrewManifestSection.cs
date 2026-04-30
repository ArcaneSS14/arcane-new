using Content.Shared.CrewManifest;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using System.Numerics;
using Content.Shared.Roles;
// Arcane-Start
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
// Arcane-End

namespace Content.Client.CrewManifest.UI;

public sealed class CrewManifestSection : BoxContainer
{
    public CrewManifestSection(
        IPrototypeManager prototypeManager,
        SpriteSystem spriteSystem,
        IClipboardManager clipboardManager, // Arcane
        DepartmentPrototype section,
        List<CrewManifestEntry> entries)
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;

        AddChild(new Label()
        {
            StyleClasses = { "LabelBig" },
            Text = Loc.GetString(section.Name)
        });

        var gridContainer = new GridContainer()
        {
            HorizontalExpand = true,
            Columns = 2
        };

        AddChild(gridContainer);

        foreach (var entry in entries)
        {
            var name = new RichTextLabel()
            {
                HorizontalExpand = true,
            };
            name.SetMessage(entry.Name);

            var titleContainer = new BoxContainer()
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true
            };

            var title = new RichTextLabel();
            title.SetMessage(entry.JobTitle);


            if (prototypeManager.TryIndex<JobIconPrototype>(entry.JobIcon, out var jobIcon))
            {
                var icon = new TextureRect()
                {
                    TextureScale = new Vector2(2, 2),
                    VerticalAlignment = VAlignment.Center,
                    Texture = spriteSystem.Frame0(jobIcon.Icon),
                    Margin = new Thickness(0, 0, 4, 0)
                };

                titleContainer.AddChild(icon);
                titleContainer.AddChild(title);
            }
            else
            {
                titleContainer.AddChild(title);
            }

            // Arcane-Edit-Start
            var nameButton = new ContainerButton
            {
                HorizontalExpand = true,
                StyleBoxOverride = new StyleBoxEmpty(),
            };
            nameButton.AddChild(name);
            nameButton.OnButtonDown += _ => clipboardManager.SetText(entry.Name);

            var titleButton = new ContainerButton
            {
                HorizontalExpand = true,
                StyleBoxOverride = new StyleBoxEmpty(),
            };
            titleButton.AddChild(titleContainer);
            titleButton.OnButtonDown += _ => clipboardManager.SetText(entry.JobTitle);

            gridContainer.AddChild(nameButton);
            gridContainer.AddChild(titleButton);
            // Arcane-Edit-End
        }
    }
}
