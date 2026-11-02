using Content.Shared.CrewManifest;
using Content.Shared.StatusIcon;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using System.Numerics;
using Content.Shared.Roles;
// Arcane-Start
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
// Arcane-End

namespace Content.Client.CrewManifest.UI;

public sealed class CrewManifestSection : BoxContainer
{
    // Arcane-Start
    private const float ManifestIconSize = 16f;

    private static readonly Color HoverBackground = Color.White.WithAlpha(0.08f);
    private static readonly Color CopiedBackground = Color.LimeGreen.WithAlpha(0.4f);
    private static readonly TimeSpan CopiedFeedbackDuration = TimeSpan.FromSeconds(0.75);
    private static readonly ResPath ManifestRsiPath = new("/Textures/_Arcane/Interface/crew_manifest.rsi");
    private static readonly ResPath FailedIconPath = new("/Textures/Interface/Nano/cross.svg.png");

    private readonly Texture _copyIcon;
    private readonly Texture _copiedIcon;
    private readonly Texture _failedIcon;
    // Arcane-End

    public CrewManifestSection(
        IPrototypeManager prototypeManager,
        SpriteSystem spriteSystem,
        IClipboardManager clipboardManager, // Arcane
        DepartmentPrototype section,
        List<CrewManifestEntry> entries)
    {
        // Arcane-Start
        var resourceCache = IoCManager.Resolve<IResourceCache>();
        var manifestRsi = resourceCache.GetResource<RSIResource>(ManifestRsiPath).RSI;

        _copyIcon = manifestRsi["copy"].Frame0;
        _copiedIcon = manifestRsi["copied"].Frame0;
        _failedIcon = resourceCache.GetResource<TextureResource>(FailedIconPath).Texture;
        // Arcane-End

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
            gridContainer.AddChild(CreateCopyCell(name, entry.Name, clipboardManager));
            gridContainer.AddChild(CreateCopyCell(titleContainer, entry.JobTitle, clipboardManager));
            // Arcane-Edit-End
        }
    }

    // Arcane-Start
    /// <summary>
    ///     Wraps a manifest cell in a button that copies <paramref name="text"/> on click and
    ///     shows hover, success and failure feedback next to the cell content.
    /// </summary>
    private ContainerButton CreateCopyCell(Control content, string text, IClipboardManager clipboard)
    {
        var background = new StyleBoxFlat(Color.Transparent);
        var icon = new TextureRect
        {
            VerticalAlignment = VAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
            Modulate = Color.Transparent,
        };

        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
        };
        row.AddChild(content);
        row.AddChild(icon);

        var button = new ContainerButton
        {
            HorizontalExpand = true,
            StyleBoxOverride = background,
            DefaultCursorShape = CursorShape.Hand,
        };
        button.AddChild(row);

        var hovered = false;
        var flashing = false;

        void ShowIcon(Texture texture)
        {
            icon.Texture = texture;
            icon.TextureScale = new Vector2(ManifestIconSize / texture.Width, ManifestIconSize / texture.Height);
            icon.Modulate = Color.White;
        }

        void ResetVisuals()
        {
            background.BackgroundColor = hovered ? HoverBackground : Color.Transparent;

            if (hovered)
                ShowIcon(_copyIcon);
            else
                icon.Modulate = Color.Transparent; // keep the reserved space, hide the glyph
        }

        button.OnMouseEntered += _ =>
        {
            hovered = true;

            if (!flashing)
                ResetVisuals();
        };

        button.OnMouseExited += _ =>
        {
            hovered = false;

            if (!flashing)
                ResetVisuals();
        };

        button.OnPressed += _ =>
        {
            // IClipboardManager.SetText has no failure result, so the only detectable
            // failure is having nothing meaningful to put into the clipboard.
            var copied = !string.IsNullOrWhiteSpace(text);

            if (copied)
                clipboard.SetText(text);

            flashing = true;
            background.BackgroundColor = copied ? CopiedBackground : Color.Transparent;
            ShowIcon(copied ? _copiedIcon : _failedIcon);

            Timer.Spawn(CopiedFeedbackDuration, () =>
            {
                flashing = false;
                ResetVisuals();
            });
        };

        return button;
    }
    // Arcane-End
}
