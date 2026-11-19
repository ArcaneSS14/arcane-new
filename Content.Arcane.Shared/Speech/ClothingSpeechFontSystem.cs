using Content.Shared.Inventory;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Trauma.Common.Speech;

namespace Content.Arcane.Shared.Speech;

/// <summary>
/// Uses Trauma's existing speech and inventory relay instead of introducing another font pipeline.
/// </summary>
public sealed partial class ClothingSpeechFontSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ClothingSpeechFontComponent, InventoryRelayedEvent<SpeechFontOverrideEvent>>(OnSpeech);
        SubscribeLocalEvent<ClothingSpeechFontComponent, GetVerbsEvent<AlternativeVerb>>(OnVerbs);
    }

    private void OnSpeech(Entity<ClothingSpeechFontComponent> ent, ref InventoryRelayedEvent<SpeechFontOverrideEvent> args)
    {
        if (!ent.Comp.Enabled)
            return;

        args.Args.Font = ent.Comp.Font;
        args.Args.Color = ent.Comp.Color;
    }

    private void OnVerbs(Entity<ClothingSpeechFontComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("arcane-clothing-speech-font-toggle"),
            Act = () =>
            {
                if (TerminatingOrDeleted(ent))
                    return;
                ent.Comp.Enabled = !ent.Comp.Enabled;
                Dirty(ent);
                _popup.PopupEntity(Loc.GetString(ent.Comp.Enabled
                    ? "arcane-clothing-speech-font-enabled" : "arcane-clothing-speech-font-disabled"), ent, user);
            }
        });
    }
}
