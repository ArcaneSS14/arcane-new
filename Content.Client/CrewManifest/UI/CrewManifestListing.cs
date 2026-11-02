using Content.Shared.CrewManifest;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.CrewManifest.UI;

public sealed partial class CrewManifestListing : BoxContainer
{
    [Dependency] private IEntitySystemManager _entitySystem = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    private readonly SpriteSystem _spriteSystem;

    // Arcane-Start
    private CrewManifestEntries? _entries;
    private LineEdit? _searchBar;

    public LineEdit? SearchBar
    {
        get => _searchBar;
        set
        {
            if (_searchBar != null)
                _searchBar.OnTextChanged -= OnSearchChanged;

            _searchBar = value;

            if (_searchBar != null)
                _searchBar.OnTextChanged += OnSearchChanged;
        }
    }
    // Arcane-End

    public CrewManifestListing()
    {
        IoCManager.InjectDependencies(this);
        _spriteSystem = _entitySystem.GetEntitySystem<SpriteSystem>();
    }

    public void AddCrewManifestEntries(CrewManifestEntries entries)
    {
        // Arcane-Edit-Start: cache entries so search changes can rebuild the listing
        _entries = entries;
        Refresh();
    }

    public void ClearCrewManifestEntries()
    {
        _entries = null;
        RemoveAllChildren();
    }

    private void OnSearchChanged(LineEdit.LineEditEventArgs args)
    {
        Refresh();
    }

    private void Refresh()
    {
        RemoveAllChildren();

        if (_entries == null)
            return;

        var filter = _searchBar?.Text.Trim();
        // Arcane-Edit-End
        var entryDict = new Dictionary<DepartmentPrototype, List<CrewManifestEntry>>();

        foreach (var entry in _entries.Entries) // Arcane-Edit: entries.Entries > _entries.Entries
        {
            // Arcane-Start
            if (!string.IsNullOrEmpty(filter) &&
                !entry.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) &&
                !entry.JobTitle.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;
            // Arcane-End

            foreach (var department in _prototypeManager.EnumeratePrototypes<DepartmentPrototype>())
            {
                // this is a little expensive, and could be better
                if (department.Roles.Contains(entry.JobPrototype))
                {
                    entryDict.GetOrNew(department).Add(entry);
                }
            }
        }

        // Arcane-Start
        if (entryDict.Count == 0 && !string.IsNullOrEmpty(filter))
        {
            AddChild(new Label
            {
                Text = Loc.GetString("crew-manifest-no-matches"),
                HorizontalExpand = true,
            });
            return;
        }
        // Arcane-End

        var entryList = new List<(DepartmentPrototype section, List<CrewManifestEntry> entries)>();

        foreach (var (section, listing) in entryDict)
        {
            entryList.Add((section, listing));
        }

        entryList.Sort((a, b) => DepartmentUIComparer.Instance.Compare(a.section, b.section));

        foreach (var item in entryList)
        {
            AddChild(new CrewManifestSection(_prototypeManager, _spriteSystem, item.section, item.entries));
        }
    }
}
