using System.Text.Json;
using Content.Arcane.Shared.Paint;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Robust.Shared.Player;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Arcane.Server.Paint;

public sealed partial class PaintLogSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IAdminManager _admin = default!;

    [Dependency] private IGameTiming _timing = default!;

    private readonly Dictionary<string, PaintImage> _roundImages = new();
    private readonly Queue<string> _imageOrder = new();
    private readonly Dictionary<NetUserId, TimeSpan> _nextRequest = new();
    private readonly HashSet<NetUserId> _pending = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeNetworkEvent<PaintLogImageRequestEvent>(OnImageRequest);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _roundImages.Clear();
        _imageOrder.Clear();
        _nextRequest.Clear();
    }

    public void LogPainting(EntityUid surface, EntityUid user, int pixels, PaintImage image)
    {
        var handler = new LogStringHandler(40, 3, _adminLog, out var enabled);
        if (!enabled)
            return;

        handler.AppendFormatted(ToPrettyString(user), "user");
        handler.AppendLiteral(" painted ");
        handler.AppendFormatted(pixels, "pixels");
        handler.AppendLiteral(" pixels on ");
        handler.AppendFormatted(ToPrettyString(surface), "surface");
        Commit(ref handler, image, LogImpact.Low);
    }

    public void LogFinished(EntityUid surface, EntityUid user, string title, PaintImage image)
    {
        var handler = new LogStringHandler(40, 2, _adminLog, out var enabled);
        if (!enabled)
            return;

        handler.AppendFormatted(ToPrettyString(user), "user");
        handler.AppendLiteral(" finished and signed ");
        handler.AppendFormatted(ToPrettyString(surface), "surface");
        handler.AppendLiteral(" as \"");
        handler.AppendLiteral(title);
        handler.AppendLiteral("\"");
        handler.Values["title"] = title;
        Commit(ref handler, image, LogImpact.Low);
    }

    public void LogCopied(EntityUid source, EntityUid copy, EntityUid? user, PaintImage image)
    {
        var handler = new LogStringHandler(40, 3, _adminLog, out var enabled);
        if (!enabled)
            return;

        if (user is { } actor)
            handler.AppendFormatted(ToPrettyString(actor), "user");
        else
            handler.AppendLiteral("A machine");

        handler.AppendLiteral(" copied ");
        handler.AppendFormatted(ToPrettyString(source), "source");
        handler.AppendLiteral(" into ");
        handler.AppendFormatted(ToPrettyString(copy), "copy");
        Commit(ref handler, image, LogImpact.Low);
    }

    public void LogImageLoaded(EntityUid surface, ICommonSession? admin, string url, PaintImage image)
    {
        var handler = new LogStringHandler(48, 2, _adminLog, out var enabled);
        if (!enabled)
            return;

        handler.AppendFormatted(admin, "admin");
        handler.AppendLiteral(" loaded an image from ");
        handler.AppendLiteral(url);
        handler.AppendLiteral(" onto ");
        handler.AppendFormatted(ToPrettyString(surface), "surface");
        handler.Values["url"] = url;
        Commit(ref handler, image, LogImpact.Medium);
    }

    private void Commit(ref LogStringHandler handler, PaintImage image, LogImpact impact)
    {
        var token = PaintLogToken.New();
        var snapshot = image.Clone();
        _roundImages[token] = snapshot;
        _imageOrder.Enqueue(token);
        while (_imageOrder.Count > 128)
            _roundImages.Remove(_imageOrder.Dequeue());

        handler.AppendLiteral(" (painting ");
        handler.AppendLiteral(token);
        handler.AppendLiteral(")");
        handler.Values["token"] = token;
        handler.Values["image"] = PaintImageCodec.Encode(snapshot);

        _adminLog.Add(LogType.Paint, impact, ref handler);
    }

    private async void OnImageRequest(PaintLogImageRequestEvent msg, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;
        if (!_admin.HasAdminFlag(session, AdminFlags.Logs) || !PaintLogToken.IsValid(msg.Token)
            || _pending.Contains(session.UserId)
            || _nextRequest.TryGetValue(session.UserId, out var next) && _timing.RealTime < next)
            return;

        _nextRequest[session.UserId] = _timing.RealTime + TimeSpan.FromSeconds(1);
        if (_roundImages.TryGetValue(msg.Token, out var cached))
        {
            RaiseNetworkEvent(new PaintLogImageResponseEvent(msg.Token, cached), session);
            return;
        }

        var filter = new LogFilter
        {
            Round = msg.Round > 0 ? msg.Round : null,
            Types = new HashSet<LogType> { LogType.Paint },
            Search = msg.Token,
            Limit = 1,
        };

        PaintImage? image = null;
        _pending.Add(session.UserId);
        try
        {
            await foreach (var json in _adminLog.AllJson(filter))
            {
                using (json)
                {
                    if (json.RootElement.TryGetProperty("token", out var token)
                        && token.ValueKind == JsonValueKind.String && token.GetString() == msg.Token
                        && json.RootElement.TryGetProperty("image", out var property)
                        && property.ValueKind == JsonValueKind.String
                        && property.GetString() is { } encoded)
                        image = PaintImageCodec.Decode(encoded);
                }

                break;
            }
        }
        catch (Exception e)
        {
            Log.Error($"Failed to load painting {msg.Token} from admin logs: {e}");
        }

        finally
        {
            _pending.Remove(session.UserId);
        }

        if (_admin.HasAdminFlag(session, AdminFlags.Logs))
            RaiseNetworkEvent(new PaintLogImageResponseEvent(msg.Token, image), session);
    }
}
