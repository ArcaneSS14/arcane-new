// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Controls;
using Content.Trauma.Common.Knowledge; // Arcane
using Content.Trauma.Common.Knowledge.Prototypes; // Arcane
using Robust.Shared.Prototypes; // Arcane
using Robust.Shared.Timing; // Arcane

namespace Content.Trauma.Client.Knowledge.UI;

[GenerateTypedNameReferences]
public sealed partial class KnowledgeTab : Control
{
    [Dependency] private IEntitySystemManager _system = default!;
    [Dependency] private IPrototypeManager _proto = default!; // Arcane
    private readonly KnowledgeSystem _knowledge;
    private readonly SpriteSystem _sprite;
    // Arcane-Start
    private const float RefreshDelay = 0.25f;
    private const int ExpertMastery = 3;

    private static readonly ProtoId<KnowledgeCategoryPrototype> LanguageCategory = "Language";

    private readonly Dictionary<ProtoId<KnowledgeCategoryPrototype>, (Collapsible Group, BoxContainer Stack)> _groups = new();
    private readonly Dictionary<(ProtoId<KnowledgeCategoryPrototype>, string), KnowledgeEntry> _entries = new();
    private readonly HashSet<(ProtoId<KnowledgeCategoryPrototype>, string)> _current = new();
    private EntityUid? _player;
    private float _nextRefresh;
    // Arcane-End

    public KnowledgeTab()
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _knowledge = _system.GetEntitySystem<KnowledgeSystem>();
        _sprite = _system.GetEntitySystem<SpriteSystem>();
    }

    // Arcane-Edit-Start: rebuild everything once > update existing controls while the tab is visible
    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _nextRefresh -= args.DeltaSeconds;
        if (_nextRefresh > 0f || _player is not { } player)
            return;

        _nextRefresh = RefreshDelay;
        UpdateKnowledgeTab(player);
    }

    /// <summary>
    /// Updates the specificied knowledge tab with the player's current knowledge.
    /// Existing controls are kept so that opened categories and the scroll position survive.
    /// </summary>
    public void UpdateKnowledgeTab(EntityUid player)
    {
        TabContainer.SetTabTitle(this, Loc.GetString("trauma-knowledge-title"));

        _player = player;
        SetKnowledge(_knowledge.GrabAllKnowledge(player));
    }

    /// <summary>
    /// Shows a list sorted by category then name, reusing the controls of anything already shown.
    /// </summary>
    public void SetKnowledge(List<(ProtoId<KnowledgeCategoryPrototype> Category, KnowledgeInfo Info)>? knowledge)
    {
        _current.Clear();

        KnowledgePlaceholder.Visible = knowledge == null;
        KnowledgeSummary.Visible = knowledge != null;
        KnowledgeBox.SeparationOverride = 10;

        ProtoId<KnowledgeCategoryPrototype>? lastGroup = null;
        var groupIndex = -1;
        var entryIndex = 0;
        var count = 0;
        var experts = 0;
        foreach (var (groupId, info) in knowledge ?? [])
        {
            if (groupId != lastGroup)
            {
                lastGroup = groupId;
                groupIndex++;
                entryIndex = 0;
            }

            var (group, stack) = EnsureGroup(groupId);
            SetPosition(group, groupIndex);

            var key = (groupId, info.Name);
            _current.Add(key);
            if (!_entries.TryGetValue(key, out var entry))
            {
                entry = new KnowledgeEntry(_knowledge, _sprite, groupId != LanguageCategory);
                _entries[key] = entry;
                stack.AddChild(entry);
            }

            entry.SetInfo(info);
            SetPosition(entry, entryIndex++);

            if (groupId == LanguageCategory)
                continue;

            count++;
            if (_knowledge.GetMastery(info.NetLevel) >= ExpertMastery)
                experts++;
        }

        RemoveStale();
        UpdateTitles();
        KnowledgeSummary.Text = Loc.GetString("knowledge-tab-summary", ("count", count), ("experts", experts));
    }

    private (Collapsible Group, BoxContainer Stack) EnsureGroup(ProtoId<KnowledgeCategoryPrototype> id)
    {
        if (_groups.TryGetValue(id, out var existing))
            return existing;

        var body = new CollapsibleBody();
        var stack = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
            Margin = new Thickness(0, 6, 0, 2),
        };
        body.AddChild(stack);

        var group = new Collapsible(Loc.GetString(_proto.Index(id).Name), body);
        KnowledgeBox.AddChild(group);
        return _groups[id] = (group, stack);
    }

    private void UpdateTitles()
    {
        foreach (var (id, (group, stack)) in _groups)
        {
            if (group.Heading is CollapsibleHeading heading)
            {
                heading.Title = Loc.GetString("knowledge-group-title",
                    ("name", Loc.GetString(_proto.Index(id).Name)),
                    ("count", stack.ChildCount));
            }
        }
    }

    private void SetPosition(Control control, int index)
    {
        if (control.GetPositionInParent() != index)
            control.SetPositionInParent(index);
    }

    private void RemoveStale()
    {
        foreach (var (key, entry) in _entries)
        {
            if (_current.Contains(key))
                continue;

            entry.Orphan();
            _entries.Remove(key);
        }

        foreach (var (id, (group, stack)) in _groups)
        {
            if (stack.ChildCount > 0)
                continue;

            group.Orphan();
            _groups.Remove(id);
        }
    }
    // Arcane-Edit-End
}
