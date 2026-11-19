using Content.Arcane.Shared.Paint;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Quantization;
using Color = Robust.Shared.Maths.Color;
using ImageColor = SixLabors.ImageSharp.Color;
using Size = SixLabors.ImageSharp.Size;

namespace Content.Arcane.Server.Paint;

public static class PaintImageImporter
{
    public const int MaxSourceDimension = 8192;
    public const long MaxSourcePixels = 4 * 1024 * 1024;

    public static PaintImage Import(byte[] data, Vector2i size, Color background)
    {
        if (!PaintImage.IsValidSize(size))
            throw new ArgumentOutOfRangeException(nameof(size));

        var options = new DecoderOptions { MaxFrames = 1, SkipMetadata = true };
        var info = Image.Identify(options, data);
        if (info.Width > MaxSourceDimension || info.Height > MaxSourceDimension ||
            (long) info.Width * info.Height > MaxSourcePixels)
            throw new InvalidImageContentException($"Image is {info.Width}x{info.Height}; source limit is {MaxSourcePixels} pixels and {MaxSourceDimension} per side.");

        using var image = Image.Load<Rgba32>(options, data);

        var backgroundColor = ImageColor.FromPixel(new Rgba32(background.RByte, background.GByte, background.BByte));
        var resize = new ResizeOptions
        {
            Size = new Size(size.X, size.Y),
            Mode = ResizeMode.Stretch,
            Sampler = KnownResamplers.Bicubic,
        };
        var quantizer = new WuQuantizer(new QuantizerOptions
        {
            MaxColors = PaintImage.MaxPaletteSize - 1,
            Dither = null,
        });

        image.Mutate(context => context
            .Resize(resize)
            .BackgroundColor(backgroundColor)
            .Quantize(quantizer));

        var result = new PaintImage(size.X, size.Y, background);
        for (var y = 0; y < size.Y; y++)
        {
            for (var x = 0; x < size.X; x++)
            {
                var pixel = image[x, y];
                result.Pixels[y * size.X + x] = result.FindOrAddColor(new Color(pixel.R, pixel.G, pixel.B, (byte) 255), out _);
            }
        }

        return result;
    }
}
