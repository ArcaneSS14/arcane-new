using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared.Roles;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Arcane.Client.ReadyManifest;

[GenerateTypedNameReferences]
public sealed partial class ReadyManifestWindow : DefaultWindow
{
    [Dependency] private IEntitySystemManager _systems = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public ReadyManifestWindow()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);
    }

    public void Rebuild(Dictionary<ProtoId<JobPrototype>, List<string>> jobCharacters)
    {
        Listing.DisposeAllChildren();

        var sprite = _systems.GetEntitySystem<SpriteSystem>();
        var departments = _proto.EnumeratePrototypes<DepartmentPrototype>()
            .Where(d => !d.EditorHidden)
            .ToList();
        departments.Sort(DepartmentUIComparer.Instance);

        JobUIComparer.TryCreate(_proto, null, out var jobComparer);

        foreach (var department in departments)
        {
            var jobs = department.Roles
                .Where(id => jobCharacters.TryGetValue(id, out var names) && names.Count > 0)
                .Select(id => _proto.Index(id))
                .ToList();

            if (jobs.Count == 0)
                continue;

            if (jobComparer != null)
                jobs.Sort(jobComparer);

            var stripe = new StripeBack();
            stripe.AddChild(new Label
            {
                StyleClasses = { "LabelBig" },
                Text = Loc.GetString($"department-{department.ID}"),
                Align = Label.AlignMode.Center,
            });
            Listing.AddChild(stripe);

            foreach (var job in jobs)
            {
                var names = jobCharacters[job.ID];

                var header = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Horizontal };
                header.AddChild(new TextureRect
                {
                    Texture = sprite.Frame0(_proto.Index(job.Icon).Icon),
                    TextureScale = new Vector2(2, 2),
                    VerticalAlignment = VAlignment.Center,
                    Margin = new Thickness(0, 0, 4, 0),
                });
                header.AddChild(new Label { Text = $"{job.LocalizedName}: {names.Count}", HorizontalExpand = true });
                Listing.AddChild(header);

                var namesBox = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                    Margin = new Thickness(20, 0, 0, 5),
                };
                foreach (var name in names.OrderBy(n => n))
                {
                    namesBox.AddChild(new Label { Text = "• " + name, FontColorOverride = Color.LightGray });
                }

                Listing.AddChild(namesBox);
            }
        }
    }
}
