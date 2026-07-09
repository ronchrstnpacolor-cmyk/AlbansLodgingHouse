using System.ComponentModel.DataAnnotations;
using AlbansLodgingHouse.Data;
using AlbansLodgingHouse.Models;
using AlbansLodgingHouse.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace AlbansLodgingHouse.Pages.Management;

public class DecisionInput
{
    [Required]
    public string BookingReferenceNo { get; set; } = "";

    [Required]
    public string Status { get; set; } = "";

    [StringLength(500)]
    public string? Remarks { get; set; }
}

public class BookingsModel : PageModel
{
    private readonly BookingRepository _bookingRepository;
    private readonly ManagementAccessService _accessService;
    private readonly BookingNotificationService _notificationService;

    public BookingsModel(
        BookingRepository bookingRepository,
        ManagementAccessService accessService,
        BookingNotificationService notificationService)
    {
        _bookingRepository = bookingRepository;
        _accessService = accessService;
        _notificationService = notificationService;
    }

    public IReadOnlyList<BookingFormRecord> Bookings { get; set; } = Array.Empty<BookingFormRecord>();

    [BindProperty]
    public DecisionInput Decision { get; set; } = new();

    public string? StatusMessage { get; set; }
    public bool StatusIsError { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await IsAllowedAsync()) return NotFound();

        Bookings = await _bookingRepository.GetAllBookingsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!await IsAllowedAsync()) return NotFound();

        ModelState.Clear();
        if (!TryValidateModel(Decision, nameof(Decision)))
        {
            StatusIsError = true;
            StatusMessage = "Please fix the errors below.";
            Bookings = await _bookingRepository.GetAllBookingsAsync();
            return Page();
        }

        try
        {
            await _bookingRepository.InsertApprovalAsync(
                Decision.BookingReferenceNo,
                Decision.Status,
                Decision.Remarks,
                Request.Host.Host);

            var booking = await _bookingRepository.GetByReferenceNoAsync(Decision.BookingReferenceNo);
            if (booking is not null)
            {
                if (Decision.Status == "Approved")
                {
                    var confirmUrl = Url.Page("/Booking/Confirm", null, new { newId = booking.NewID }, Request.Scheme, Request.Host.Value) ?? "";
                    await _notificationService.SendClientApprovedEmailAsync(booking, confirmUrl);
                }
                else
                {
                    await _notificationService.SendClientDisapprovedEmailAsync(booking, Decision.Remarks);
                }
            }

            StatusMessage = $"Booking {Decision.BookingReferenceNo} marked {Decision.Status}.";
        }
        catch (SqlException ex)
        {
            StatusIsError = true;
            StatusMessage = ex.Message;
        }

        Bookings = await _bookingRepository.GetAllBookingsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCheckoutAsync(Guid newId)
    {
        if (!await IsAllowedAsync()) return NotFound();

        try
        {
            var booking = await _bookingRepository.CheckoutAsync(newId, Request.Host.Host);
            StatusMessage = $"Booking {booking.BookingReferenceNo} marked as checked out.";
        }
        catch (SqlException ex)
        {
            StatusIsError = true;
            StatusMessage = ex.Message;
        }

        Bookings = await _bookingRepository.GetAllBookingsAsync();
        return Page();
    }

    private async Task<bool> IsAllowedAsync()
    {
        return await _accessService.IsHostAllowedAsync(Request.Host.Host);
    }
}
