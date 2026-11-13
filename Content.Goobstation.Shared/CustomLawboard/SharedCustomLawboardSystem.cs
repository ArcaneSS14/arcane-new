// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;


namespace Content.Goobstation.Shared.CustomLawboard;

public abstract partial class SharedCustomLawboardSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public static readonly int MaxLaws = 15;
    public static readonly int MaxLawLength = 512; // These 2 are random arbitrary numbers (These don't seem like they're worth making cvars for)
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CustomLawboardComponent, CustomLawboardChangeLawsMessage>(OnChangeLaws);
        SubscribeLocalEvent<CustomLawboardComponent, MapInitEvent>(OnLawboardMapInit);
    }

    private void OnLawboardMapInit(Entity<CustomLawboardComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Laws.Count == 0 &&
            TryComp<SiliconLawProviderComponent>(ent, out var provider) &&
            provider.Lawset is { } lawset)
            ent.Comp.Laws = SanitizeLaws(lawset.Laws);
    }

    public static List<SiliconLaw> SanitizeLaws(IReadOnlyList<SiliconLaw> laws) // Arcane
    {
        var count = Math.Min(laws.Count, MaxLaws);
        var sanitizedLaws = new List<SiliconLaw>(count);
        for (var i = 0; i < count; i++)
        {
            var law = laws[i];
            if (law == null)
                continue;

            sanitizedLaws.Add(new SiliconLaw()
            {
                LawString = SanitizeText(law.LawString ?? string.Empty),
                Order = law.Order,
                LawIdentifierOverride = law.LawIdentifierOverride is { } label ? SanitizeText(label) : null,
            });
        }
        return sanitizedLaws;
    }

    private static string SanitizeText(string text)
    {
        if (text.Length > MaxLawLength)
        {
            var length = char.IsHighSurrogate(text[MaxLawLength - 1]) ? MaxLawLength - 1 : MaxLawLength;
            text = text[..length];
        }

        return text.Replace('\r', ' ').Replace('\n', ' ');
    }

    private void OnChangeLaws(EntityUid uid, CustomLawboardComponent customLawboard, CustomLawboardChangeLawsMessage args)
    {
        var provider = EnsureComp<SiliconLawProviderComponent>(uid);
        var sanitizedLaws = SanitizeLaws(args.Laws);
        var lawset = new SiliconLawset
        {
            Laws = sanitizedLaws,
            ObeysTo = provider.Lawset?.ObeysTo ?? string.Empty,
        };

        customLawboard.Laws = sanitizedLaws;
        provider.Lawset = lawset;
        var lawTexts = new List<string>(lawset.Laws.Count);
        foreach (var law in lawset.Laws)
        {
            lawTexts.Add($"{law.Order}: {law.LawString}");
        }
        var joined = string.Join(", ", lawTexts);
        _adminLogger.Add(LogType.Action, $"{args.Actor:player} changed laws on {uid} to: {joined}");
        Dirty(uid, customLawboard);

        if (args.Popup)
        {
            _popup.PopupEntity(Loc.GetString("custom-lawboard-updated"), args.Actor, args.Actor); // This is entirely to make the UI feel responsive
        }
    }

    protected virtual void DirtyUI(EntityUid uid, CustomLawboardComponent? customLawboard, UserInterfaceComponent? ui = null) { }
}
