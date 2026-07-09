using System.Net;
using AlbansLodgingHouse.Data;
using AlbansLodgingHouse.Models;

namespace AlbansLodgingHouse.Services;

public class BookingNotificationService
{
    private readonly EmailService _emailService;
    private readonly ManagementEmailService _managementEmailService;
    private readonly BookingCardImageService _cardImageService;
    private readonly QrCodeService _qrCodeService;

    public BookingNotificationService(
        EmailService emailService,
        ManagementEmailService managementEmailService,
        BookingCardImageService cardImageService,
        QrCodeService qrCodeService)
    {
        _emailService = emailService;
        _managementEmailService = managementEmailService;
        _cardImageService = cardImageService;
        _qrCodeService = qrCodeService;
    }

    public async Task NotifyManagementNewBookingAsync(BookingFormRecord booking, string managementUrl, CancellationToken cancellationToken = default)
    {
        var recipients = await _managementEmailService.GetActiveEmailsAsync(cancellationToken);
        var subject = $"New booking request — {booking.BookingReferenceNo}";
        var html =
            $"""
            <p>A new reservation request has been submitted and is awaiting review.</p>
            <ul>
                <li><strong>Ref:</strong> {booking.BookingReferenceNo}</li>
                <li><strong>Name:</strong> {WebUtility.HtmlEncode(booking.FullName)}</li>
                <li><strong>Contact:</strong> {WebUtility.HtmlEncode(booking.PhoneNo ?? booking.Email ?? "-")}</li>
                <li><strong>Check-in:</strong> {FormatDate(booking.CheckIn)}</li>
                <li><strong>Check-out:</strong> {FormatDate(booking.CheckOut)}</li>
                <li><strong>Room:</strong> {WebUtility.HtmlEncode(booking.RoomType ?? "-")}</li>
                <li><strong>Pax:</strong> {booking.Pax}</li>
            </ul>
            <p><a href="{managementUrl}">Review this request</a></p>
            """;

        foreach (var recipient in recipients)
        {
            await _emailService.SendAsync(recipient.Email, recipient.FullName, subject, html, cancellationToken: cancellationToken);
        }
    }

    public async Task SendClientApprovedEmailAsync(BookingFormRecord booking, string confirmUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(booking.Email)) return;

        var qrText = QrCodeService.BuildBookingQrText(booking);
        var qrBytes = _qrCodeService.GeneratePngBytes(qrText);
        var jpegBytes = _cardImageService.GenerateJpeg(booking, qrBytes);

        var subject = "Your booking has been approved — Alban's Lodging House";
        var html =
            $"""
            <p>Hi {WebUtility.HtmlEncode(booking.FullName)},</p>
            <p>Good news — your reservation request has been <strong>approved</strong>.</p>
            <ul>
                <li><strong>Ref:</strong> {booking.BookingReferenceNo}</li>
                <li><strong>Check-in:</strong> {FormatDate(booking.CheckIn)}</li>
                <li><strong>Check-out:</strong> {FormatDate(booking.CheckOut)}</li>
                <li><strong>Room:</strong> {WebUtility.HtmlEncode(booking.RoomType ?? "-")}</li>
                <li><strong>Pax:</strong> {booking.Pax}</li>
            </ul>
            <p>Please confirm your reservation so our front desk knows to expect you:</p>
            <p>
                <a href="{confirmUrl}" style="display:inline-block;padding:12px 22px;background:#1F3D2B;color:#fff;text-decoration:none;border-radius:6px;font-weight:600;">
                    Confirm My Reservation
                </a>
            </p>
            <p>Your booking card (with QR code) is attached — save it for your records or show it at check-in.</p>
            """;

        await _emailService.SendAsync(
            booking.Email!,
            booking.FullName,
            subject,
            html,
            new[] { new EmailAttachment($"{booking.BookingReferenceNo}.jpg", jpegBytes, "image/jpeg") },
            cancellationToken);
    }

    public async Task SendClientDisapprovedEmailAsync(BookingFormRecord booking, string? remarks, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(booking.Email)) return;

        var subject = "Update on your booking request — Alban's Lodging House";
        var reasonHtml = string.IsNullOrWhiteSpace(remarks)
            ? ""
            : $"<p><strong>Reason:</strong> {WebUtility.HtmlEncode(remarks)}</p>";

        var html =
            $"""
            <p>Hi {WebUtility.HtmlEncode(booking.FullName)},</p>
            <p>We're sorry — we're unable to accommodate your reservation request (Ref: {booking.BookingReferenceNo}).</p>
            {reasonHtml}
            <p>Please feel free to contact us if you'd like to try different dates or a different room type.</p>
            """;

        await _emailService.SendAsync(booking.Email!, booking.FullName, subject, html, cancellationToken: cancellationToken);
    }

    public async Task NotifyManagementClientConfirmedAsync(BookingFormRecord booking, CancellationToken cancellationToken = default)
    {
        var recipients = await _managementEmailService.GetActiveEmailsAsync(cancellationToken);
        var subject = $"Guest confirmed booking — {booking.BookingReferenceNo}";
        var html =
            $"""
            <p>{WebUtility.HtmlEncode(booking.FullName)} has confirmed their reservation (Ref {booking.BookingReferenceNo})
            for {FormatDate(booking.CheckIn)} – {FormatDate(booking.CheckOut)},
            {WebUtility.HtmlEncode(booking.RoomType ?? "-")}, {booking.Pax} pax.</p>
            """;

        foreach (var recipient in recipients)
        {
            await _emailService.SendAsync(recipient.Email, recipient.FullName, subject, html, cancellationToken: cancellationToken);
        }
    }

    private static string FormatDate(DateOnly? date) => date?.ToString("yyyy-MM-dd") ?? "-";
}
