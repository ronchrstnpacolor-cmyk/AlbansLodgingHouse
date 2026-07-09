using AlbansLodgingHouse.Models;
using SkiaSharp;

namespace AlbansLodgingHouse.Services;

public class BookingCardImageService
{
    private readonly SKTypeface _regularTypeface;
    private readonly SKTypeface _boldTypeface;

    public BookingCardImageService()
    {
        var fontsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts");
        _regularTypeface = SKTypeface.FromFile(Path.Combine(fontsDir, "PTSans-Regular.ttf"));
        _boldTypeface = SKTypeface.FromFile(Path.Combine(fontsDir, "PTSans-Bold.ttf"));
    }

    public byte[] GenerateJpeg(BookingFormRecord booking, byte[] qrPngBytes)
    {
        const int width = 900;
        const int height = 620;
        const int margin = 40;
        const int qrSize = 320;

        var green = new SKColor(0x1F, 0x3D, 0x2B);
        var muted = new SKColor(0x6B, 0x6B, 0x63);

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        using var qrBitmap = SKBitmap.Decode(qrPngBytes);
        using var qrResized = qrBitmap.Resize(new SKImageInfo(qrSize, qrSize), new SKSamplingOptions(SKFilterMode.Linear));
        canvas.DrawBitmap(qrResized, new SKPoint(margin, 110), new SKSamplingOptions(SKFilterMode.Linear));

        using var titleFont = new SKFont(_boldTypeface, 28);
        using var labelFont = new SKFont(_boldTypeface, 14);
        using var valueFont = new SKFont(_regularTypeface, 20);
        using var footerFont = new SKFont(_regularTypeface, 13);

        using var greenPaint = new SKPaint { Color = green, IsAntialias = true };
        using var mutedPaint = new SKPaint { Color = muted, IsAntialias = true };

        canvas.DrawText("Alban's Lodging House", margin, 55, SKTextAlign.Left, titleFont, greenPaint);

        var rows = new (string Label, string Value)[]
        {
            ("BOOKING REF", booking.BookingReferenceNo),
            ("NAME", booking.FullName),
            ("CHECK-IN", booking.CheckIn?.ToString("yyyy-MM-dd") ?? "-"),
            ("CHECK-OUT", booking.CheckOut?.ToString("yyyy-MM-dd") ?? "-"),
            ("ROOM TYPE", string.IsNullOrWhiteSpace(booking.RoomType) ? "-" : booking.RoomType),
            ("PAX", booking.Pax.ToString()),
        };

        float y = 140;
        const float textX = margin + qrSize + 40;
        foreach (var (label, value) in rows)
        {
            canvas.DrawText(label, textX, y, SKTextAlign.Left, labelFont, mutedPaint);
            canvas.DrawText(value, textX, y + 26, SKTextAlign.Left, valueFont, greenPaint);
            y += 70;
        }

        canvas.DrawText("Please keep this for your records.", margin, height - 30, SKTextAlign.Left, footerFont, mutedPaint);

        canvas.Flush();

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }
}
