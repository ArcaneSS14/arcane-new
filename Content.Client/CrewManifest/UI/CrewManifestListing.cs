using Content.Shared.CrewManifest;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
// Arcane-Start
using Robust.Client.Animations;
using Robust.Client.UserInterface;
using Robust.Shared.Animations;
// Arcane-End

namespace Content.Client.CrewManifest.UI;

public sealed partial class CrewManifestListing : BoxContainer
{
    [Dependency] private IEntitySystemManager _entitySystem = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    private readonly SpriteSystem _spriteSystem;

    // Arcane-Start
    private const string SearchTransitionKey = "crew-manifest-search-transition";
    private const float SearchTransitionInitialOpacity = 0.35f;
    private static readonly TimeSpan SearchTransitionDuration = TimeSpan.FromMilliseconds(200);

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

    private void Refresh(bool animate = false)
    {
        RemoveAllChildren();

        if (!animate)
            ResetSearchTransition();

        if (_entries == null)
            return;

        if (animate)
            PlaySearchTransition();

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
