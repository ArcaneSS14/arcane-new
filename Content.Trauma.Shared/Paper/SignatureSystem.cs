// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Access.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Content.Trauma.Common.Paper;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Trauma.Shared.Paper;

public sealed partial class SignatureSystem : EntitySystem
{
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private TagSystem _tag = default!;

    // The sprite used to visualize "signatures" on paper entities.
    private const string SignatureStampState = "paper_stamp-signature";

    public static readonly ProtoId<TagPrototype> PenTag = "Write";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);
    }

    private void OnGetAltVerbs(Entity<PaperComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (args.Using is not {} pen || !_tag.HasTag(pen, PenTag))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () =>
            {
                TrySignPaper(ent, user, pen);
            },
            Text = Loc.GetString("paper-sign-verb"),
            DoContactInteraction = true,
            Priority = 10
        });
    }

    /// <summary>
    ///     Tries to add a signature to the paper with signer's name.
    /// </summary>
    public bool TrySignPaper(Entity<PaperComponent> paper, EntityUid signer, EntityUid pen)
    {
        // Arcane-Edit-Start
        if (!TryGetSignatureName(paper, signer, out var signatureName))
            return false;
        // Arcane-Edit-End

        var stampInfo = new StampDisplayInfo()
        {
            StampedName = signatureName,
            StampedColor = Color.DarkSlateGray, // TODO Make this configurable depending on the pen.
        };

        if (!_paper.TryStamp(paper, stampInfo, SignatureStampState))
        {
            _popup.PopupEntity(Loc.GetString("paper-signed-failure", ("target", paper)), signer, signer, PopupType.SmallCaution);
            return false;
        }

        // Arcane-Edit-Start
        _audio.PlayPredicted(paper.Comp.Sound, signer, signer);
        NotifySigned(paper, signer);
        return true;
    }

    /// <summary>Use the same cancellable signing rules and identity for an inline signature.</summary>
    public bool TryGetSignatureName(Entity<PaperComponent> paper, EntityUid signer, out string name)
    {
        name = string.Empty;
        var attempt = new SignAttemptEvent(signer);
        RaiseLocalEvent(paper, ref attempt);
        if (attempt.Cancelled)
            return false;

        var signature = new GetSignatureEvent(paper);
        RaiseLocalEvent(signer, ref signature);
        name = signature.Signature ?? GetDefaultSignature(signer);
        return true;
    }

    /// <summary>Notify contract consumers only after a signature has actually been written.</summary>
    public void NotifySigned(Entity<PaperComponent> paper, EntityUid signer)
    {
        // Arcane-Edit-End
        _paper.UpdateUserInterface(paper);

        // devil has its own popup so let it cancel it
        var ev = new PaperSignedEvent(signer);
        RaiseLocalEvent(paper, ref ev);
        if (!ev.Handled)
        {
            var identity = Identity.Entity(signer, EntityManager);
            var you = Loc.GetString("paper-signed-self", ("target", paper));
            var others = Loc.GetString("paper-signed-other", ("user", identity), ("target", paper));
            _popup.PopupEntity(you, others, signer, signer);
        }

    }

    private string GetDefaultSignature(EntityUid uid)
    {
        // If the entity has an ID, use the name on it.
        if (_idCard.TryFindIdCard(uid, out var id) && !string.IsNullOrWhiteSpace(id.Comp.FullName))
            return id.Comp.FullName;

        // Alternatively, return the entity name
        return Name(uid);
    }
}
