using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.Chat;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;

namespace Content.Arcane.Client.Chat.DirectionalEmote;

public sealed partial class DirectionalEmoteSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IUserInterfaceManager _ui = default!;

    private static readonly SpriteSpecifier VerbIcon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/emotes.svg.192dpi.png"));

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DirectionalEmoteTargetComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    public void Send(NetEntity target, string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > _cfg.GetCVar(ArcaneCVars.DirectionalEmoteMaxLength))
            return;

        RaiseNetworkEvent(new SendDirectionalEmoteEvent(target, text));
    }

    private void OnGetVerbs(Entity<DirectionalEmoteTargetComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!_cfg.GetCVar(ArcaneCVars.DirectionalEmotesEnabled)
            || args.Target == args.User
            || !HasComp<DirectionalEmoteTargetComponent>(args.User))
            return;

        var target = GetNetEntity(args.Target);
        args.Verbs.Add(new Verb
        {
            Act = () => _ui.GetUIController<DirectionalEmoteUIController>().ToggleWindow(target),
            Priority = 15,
            Text = Loc.GetString("directional-emote-verb-name"),
            Icon = VerbIcon,
            ClientExclusive = true,
        });
    }
}
