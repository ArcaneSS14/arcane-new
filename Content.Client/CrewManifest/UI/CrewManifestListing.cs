using Content.Shared.CrewManifest;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Robust.Client.UserInterface; // Arcane

namespace Content.Client.CrewManifest.UI;

// Arcane-Start
using Robust.Client.Animations;
using Robust.Shared.Animations;
// Arcane-End

public sealed partial class CrewManifestListing : BoxContainer
{
    [Dependency] private IEntitySystemManager _entitySystem = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    private readonly SpriteSystem _spriteSystem;

    // Arcane-Start
    private const string SearchTransitionKey = "crew-manifest-search-transition";
    private const float SearchTransitionInitialOpacity = 0.35f;
    private static readonly TimeSpan SearchTransitionDuration = TimeSpan.FromMilliseconds(200);

    private List<(DepartmentPrototype Department, List<CrewManifestEntry> Entries)>? _sections;
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
        // Arcane-Edit-Start: group and sort entries once when the manifest changes
        _sections = GroupAndSortEntries(entries);
        Refresh();
        // Arcane-Edit-End
    }

    // Arcane-Start
    public void ClearCrewManifestEntries()
    {
        _sections = null;
        RemoveAllChildren();
        ResetSearchTransition();
    }

    private void OnSearchChanged(LineEdit.LineEditEventArgs args)
    {
        Refresh(animate: true);
    }

    private void PlaySearchTransition()
    {
        var initialColor = HasRunningAnimation(SearchTransitionKey)
            ? Modulate
            : Color.White.WithAlpha(SearchTransitionInitialOpacity);

        StopAnimation(SearchTransitionKey);
        Modulate = initialColor;
        PlayAnimation(new Animation
        {
            Length = SearchTransitionDuration,
            AnimationTracks =
            {
                new AnimationTrackControlProperty
                {
                    Property = nameof(Control.Modulate),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(initialColor, 0f),
                        new AnimationTrackProperty.KeyFrame(
                            Color.White,
                            (float) SearchTransitionDuration.TotalSeconds,
                            ExpressiveDefaultEffects),
                    },
                },
            },
        }, SearchTransitionKey);
    }

    private void ResetSearchTransition()
    {
        StopAnimation(SearchTransitionKey);
        Modulate = Color.White;
    }

    /// <summary>
    ///     Material 3 Expressive default-effects curve: cubic-bezier(0.34, 0.80, 0.34, 1.00).
    /// </summary>
    private static float ExpressiveDefaultEffects(float progress)
    {
        if (progress <= 0f)
            return 0f;

        if (progress >= 1f)
            return 1f;

        var lower = 0f;
        var upper = 1f;

        // Invert the Bezier's x axis to convert linear time into curve progress.
        for (var i = 0; i < 10; i++)
        {
            var parameter = (lower + upper) * 0.5f;
            if (CubicBezierCoordinate(parameter, 0.34f, 0.34f) < progress)
                lower = parameter;
            else
                upper = parameter;
        }

        return CubicBezierCoordinate((lower + upper) * 0.5f, 0.80f, 1f);
    }

    private static float CubicBezierCoordinate(float parameter, float firstControlPoint, float secondControlPoint)
    {
        var inverse = 1f - parameter;
        return 3f * inverse * inverse * parameter * firstControlPoint
            + 3f * inverse * parameter * parameter * secondControlPoint
            + parameter * parameter * parameter;
    }

    private List<(DepartmentPrototype Department, List<CrewManifestEntry> Entries)> GroupAndSortEntries(
        CrewManifestEntries entries)
    {
        var entriesByDepartment = new Dictionary<string, List<CrewManifestEntry>>();
        var crewManifestSystem = _entitySystem.GetEntitySystem<CrewManifestSystem>();

        foreach (var entry in entries.Entries)
        {
            foreach (var departmentId in crewManifestSystem.GetDepartmentsForJob(entry.JobPrototype))
            {
                entriesByDepartment.GetOrNew(departmentId).Add(entry);
            }
        }

        var sections = new List<(DepartmentPrototype Department, List<CrewManifestEntry> Entries)>(
            entriesByDepartment.Count);

        foreach (var (departmentId, departmentEntries) in entriesByDepartment)
        {
            if (_prototypeManager.TryIndex<DepartmentPrototype>(departmentId, out var department))
                sections.Add((department, departmentEntries));
        }

        sections.Sort((a, b) => DepartmentUIComparer.Instance.Compare(a.Department, b.Department));
        return sections;
    }

    private void Refresh(bool animate = false)
    {
        RemoveAllChildren();

        if (!animate)
            ResetSearchTransition();

        if (_sections == null)
            return;

        if (animate)
            PlaySearchTransition();

        var filter = _searchBar?.Text.Trim() ?? string.Empty;
        var hasFilter = filter.Length > 0;
        var hasMatches = false;

        foreach (var (department, entries) in _sections)
        {
            if (!hasFilter)
            {
                AddChild(new CrewManifestSection(_prototypeManager, _spriteSystem, department, entries));
                continue;
            }

            var matchingEntries = new List<CrewManifestEntry>();
            foreach (var entry in entries)
            {
                if (entry.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    entry.JobTitle.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    matchingEntries.Add(entry);
            }

            if (matchingEntries.Count == 0)
                continue;

            hasMatches = true;
            AddChild(new CrewManifestSection(_prototypeManager, _spriteSystem, department, matchingEntries));
        }

        if (hasFilter && !hasMatches)
        {
            AddChild(new Label
            {
                Text = Loc.GetString("crew-manifest-no-matches"),
                HorizontalExpand = true,
            });
        }
    }
    // Arcane-End
}
