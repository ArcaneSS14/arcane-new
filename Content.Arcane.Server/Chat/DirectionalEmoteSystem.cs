using Content.Arcane.Common.CCVar;
using Content.Arcane.Shared.Chat;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Examine;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Chat;

/// <summary>
/// Emotes seen only by the sender and one target in range.
/// </summary>
public sealed partial class DirectionalEmoteSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SendDirectionalEmoteEvent>(OnSendDirectionalEmote);
    }

    private void OnSendDirectionalEmote(SendDirectionalEmoteEvent args, EntitySessionEventArgs session)
    {
        if (!_cfg.GetCVar(ArcaneCVars.DirectionalEmotesEnabled) || string.IsNullOrWhiteSpace(args.Text))
            return;

        if (session.SenderSession.AttachedEntity is not { } source
            || !TryGetEntity(args.Target, out var target)
            || target == source
            || !TryComp<ActorComponent>(target, out var targetActor)
            || !TryComp<DirectionalEmoteTargetComponent>(source, out var emote)
            || !HasComp<DirectionalEmoteTargetComponent>(target))
            return;

        var now = _timing.CurTime;
        if (emote.LastSend + emote.Cooldown > now)
            return;

        var channel = session.SenderSession.Channel;
        if (!_examine.InRangeUnOccluded(source, target.Value, _cfg.GetCVar(ArcaneCVars.DirectionalEmoteRange)))
        {
            var rangeError = Loc.GetString("directional-emote-range-error");
            _chat.ChatMessageToOne(ChatChannel.Emotes, rangeError, rangeError, default, false, channel);
            return;
        }

        var maxLength = _cfg.GetCVar(ArcaneCVars.DirectionalEmoteMaxLength);
        if (args.Text.Length > maxLength)
        {
            var lengthError = Loc.GetString("directional-emote-length-error", ("maxLength", maxLength));
            _chat.ChatMessageToOne(ChatChannel.Emotes, lengthError, lengthError, default, false, channel);
            return;
        }

        var text = args.Text.Trim();
        var wrapped = Loc.GetString("chat-manager-entity-directional-emote-wrap-message",
            ("entityName", FormattedMessage.EscapeText(Name(source))),
            ("message", FormattedMessage.EscapeText(text)));

        _chat.ChatMessageToMany(ChatChannel.Emotes, text, wrapped, source, false, true,
            [targetActor.PlayerSession.Channel, channel]);

        emote.LastSend = now;
        _adminLogger.Add(LogType.Chat, LogImpact.Low,
            $"{ToPrettyString(source):source} sent a directional emote to {ToPrettyString(target):target}: {text}");
    }
}
