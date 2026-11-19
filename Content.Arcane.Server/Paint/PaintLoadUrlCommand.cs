using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Content.Arcane.Shared.Paint;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Network;
using SixLabors.ImageSharp;

namespace Content.Arcane.Server.Paint;

[AdminCommand(AdminFlags.Fun)]
public sealed partial class PaintLoadUrlCommand : LocalizedEntityCommands
{
    [Dependency] private IAdminManager _admin = default!;
    private bool _busy;

    [Dependency] private PaintSurfaceSystem _paint = default!;
    [Dependency] private PaintLogSystem _paintLog = default!;

    public override string Command => "paintloadurl";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is { } player && !_admin.HasAdminFlag(player, AdminFlags.Fun))
            return;

        if (_busy)
        {
            shell.WriteError(Loc.GetString("cmd-paintloadurl-busy"));
            return;
        }

        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity)
            || !EntityManager.TryGetEntity(netEntity, out var target)
            || !EntityManager.HasComponent<PaintSurfaceComponent>(target))
        {
            shell.WriteError(Loc.GetString("cmd-paintloadurl-bad-target", ("target", args[0])));
            return;
        }

        if (!Uri.TryCreate(args[1], UriKind.Absolute, out var uri)
            || !PaintImageDownloader.IsAllowedUri(uri))
        {
            shell.WriteError(Loc.GetString("cmd-paintloadurl-bad-url"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-paintloadurl-downloading", ("url", uri.GetLeftPart(UriPartial.Authority))));

        var surface = EntityManager.GetComponent<PaintSurfaceComponent>(target.Value);
        var size = surface.Size;
        var background = surface.Background;

        PaintImage image;
        _busy = true;
        try
        {
            var data = await PaintImageDownloader.Download(uri);
            image = await Task.Run(() => PaintImageImporter.Import(data, size, background));
        }
        catch (Exception e) when (e is HttpRequestException or OperationCanceledException or InvalidDataException or IOException)
        {
            shell.WriteError(Loc.GetString("cmd-paintloadurl-download-failed", ("error", e.GetType().Name)));
            return;
        }
        catch (Exception e) when (e is ImageFormatException or NotSupportedException)
        {
            shell.WriteError(Loc.GetString("cmd-paintloadurl-bad-image", ("error", e.Message)));
            return;
        }

        finally
        {
            _busy = false;
        }

        if (shell.Player is { } currentPlayer && !_admin.HasAdminFlag(currentPlayer, AdminFlags.Fun))
            return;

        if (!EntityManager.TryGetComponent<PaintSurfaceComponent>(target, out var current)
            || !ReferenceEquals(surface, current) || current.Size != size || current.Background != background
            || current.LifeStage >= ComponentLifeStage.Stopping || EntityManager.IsQueuedForDeletion(target.Value)
            || !_paint.SetImage((target.Value, current), image))
        {
            shell.WriteError(Loc.GetString("cmd-paintloadurl-bad-target", ("target", args[0])));
            return;
        }

        _paintLog.LogImageLoaded(target.Value, shell.Player, uri.GetLeftPart(UriPartial.Authority), image);
        shell.WriteLine(Loc.GetString("cmd-paintloadurl-success",
            ("target", EntityManager.ToPrettyString(target.Value))));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint(Loc.GetString("cmd-paintloadurl-hint-target")),
            2 => CompletionResult.FromHint(Loc.GetString("cmd-paintloadurl-hint-url")),
            _ => CompletionResult.Empty,
        };
    }
}
