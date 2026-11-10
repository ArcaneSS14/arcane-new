using Content.Arcane.Shared.Paper;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Trauma.Shared.Paper;
using Robust.Shared.Audio.Systems;

namespace Content.Arcane.Server.Paper;

/// <summary>Goob paper fields, adapted to Trauma's authoritative writing and signing paths.</summary>
public sealed partial class PaperFieldSystem : EntitySystem
{
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SignatureSystem _signatures = default!;
    [Dependency] private ISharedAdminLogManager _logs = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PaperComponent, PaperFieldMessage>(OnFill);
        SubscribeLocalEvent<PaperComponent, PaperStampAppliedEvent>(OnStamped);
    }

    private void OnFill(Entity<PaperComponent> paper, ref PaperFieldMessage args)
    {
        if (!TryFill(paper, args.Actor, args.Kind, args.Index, args.Value, args.ExpectedContent))
        {
            _popup.PopupEntity(Loc.GetString("paper-field-rejected"), paper, args.Actor);
            _paper.UpdateUserInterface(paper);
        }
    }

    /// <summary>Apply a single current field; never accept a client-provided replacement document or signature.</summary>
    public bool TryFill(Entity<PaperComponent> paper, EntityUid actor, PaperFieldKind kind, int index,
        string value, string expectedContent)
    {
        if (value == null || expectedContent == null || value.Length > paper.Comp.ContentSize ||
            expectedContent.Length > paper.Comp.ContentSize || expectedContent != paper.Comp.Content ||
            !Enum.IsDefined(kind) || index < 0 || !_paper.CanWrite(paper, actor, out var pen, signing: kind == PaperFieldKind.Signature))
            return false;

        var attempt = new PaperWriteAttemptEvent(paper);
        RaiseLocalEvent(actor, ref attempt);
        if (attempt.Cancelled)
            return false;

        if (kind == PaperFieldKind.Signature)
        {
            if (value.Length != 0 || !_signatures.TryGetSignatureName(paper, actor, out value))
                return false;
        }

        if (!PaperFields.TryFill(paper.Comp.Content, kind, index, value, paper.Comp.ContentSize, out var text))
            return false;

        var written = new PaperWriteEvent(actor, paper);
        RaiseLocalEvent(pen.Value, ref written);
        paper.Comp.Mode = PaperComponent.PaperAction.Read;
        _paper.SetContent(paper, text, actor);
        _logs.Add(LogType.Chat, LogImpact.Low,
            $"{ToPrettyString(actor):player} filled a {kind} field on {ToPrettyString(paper):entity}: {value}");

        if (kind == PaperFieldKind.Signature)
            _signatures.NotifySigned(paper, actor);
        else
            _audio.PlayPvs(paper.Comp.Sound, paper);
        return true;
    }

    private void OnStamped(Entity<PaperComponent> paper, ref PaperStampAppliedEvent args)
    {
        var text = PaperFields.FinalizeDocument(paper.Comp.Content);
        if (text != paper.Comp.Content)
            _paper.SetContent(paper, text);
    }
}
