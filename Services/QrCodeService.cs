using AlbansLodgingHouse.Models;
using QRCoder;

namespace AlbansLodgingHouse.Services;

public class QrCodeService
{
    public byte[] GeneratePngBytes(string content)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        var pngQrCode = new PngByteQRCode(data);
        return pngQrCode.GetGraphic(10);
    }

    public string GeneratePngDataUri(string content)
    {
        return $"data:image/png;base64,{Convert.ToBase64String(GeneratePngBytes(content))}";
    }

    public static string BuildBookingQrText(BookingFormRecord booking) =>
        $"""
        Alban's Lodging House
        Ref: {booking.BookingReferenceNo}
        Name: {booking.FullName}
        Check-in: {(booking.CheckIn is { } ci ? ci.ToString("yyyy-MM-dd") : "-")}
        Check-out: {(booking.CheckOut is { } co ? co.ToString("yyyy-MM-dd") : "-")}
        Room: {(string.IsNullOrWhiteSpace(booking.RoomType) ? "-" : booking.RoomType)}
        Pax: {booking.Pax}
        """;
}
