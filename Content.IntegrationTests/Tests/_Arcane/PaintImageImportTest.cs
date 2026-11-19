using System.IO;
using System.Net.Http;
using Content.Arcane.Server.Paint;
using NUnit.Framework;
using Robust.Shared.Maths;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Robust.Shared.Maths.Color;

namespace Content.IntegrationTests.Tests._Arcane;

// This boundary test needs neither a game instance nor external network access.
[TestFixture]
public sealed class PaintImageImportTest
{
    [Test]
    public void RejectsPrivateHostsAndOversizedImagesBeforeImporting()
    {
        foreach (var url in new[]
                 {
                     "file:///image.png", "http://user:password@example.com/image.png",
                     "http://127.1/", "http://2130706433/", "http://10.0.0.1/", "http://100.64.0.1/",
                     "http://169.254.169.254/", "http://172.16.0.1/", "http://192.168.0.1/",
                     "http://[::1]/", "http://[::ffff:127.0.0.1]/", "http://[fc00::1]/", "http://[fe80::1]/",
                 })
        {
            var uri = new Uri(url);
            Assert.That(PaintImageDownloader.IsAllowedUri(uri), Is.False, url);
            Assert.ThrowsAsync<InvalidDataException>(() => PaintImageDownloader.Download(uri), url);
        }

        // A hostname must also be checked after DNS resolution, before opening a socket.
        Assert.ThrowsAsync<HttpRequestException>(() => PaintImageDownloader.Download(new Uri("http://localhost/")));
        Assert.That(PaintImageDownloader.IsAllowedUri(new Uri("https://example.com/image.png")), Is.True);

        using var data = new MemoryStream();
        using (var oversized = new Image<Rgba32>(2049, 2048))
            oversized.SaveAsPng(data);
        Assert.Throws<InvalidImageContentException>(() =>
            PaintImageImporter.Import(data.ToArray(), new Vector2i(33, 33), Color.White));

        data.SetLength(0);
        using (var valid = new Image<Rgba32>(2, 2, new Rgba32(255, 0, 0)))
            valid.SaveAsPng(data);
        var imported = PaintImageImporter.Import(data.ToArray(), new Vector2i(33, 33), Color.White);
        Assert.That(imported.GetPixel(0), Is.EqualTo(Color.Red));
    }
}
