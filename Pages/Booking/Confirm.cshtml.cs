using AlbansLodgingHouse.Data;
using AlbansLodgingHouse.Models;
using AlbansLodgingHouse.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlbansLodgingHouse.Pages.Booking;

public class ConfirmModel : PageModel
{
    private readonly BookingRepository _bookingRepository;
    private readonly BookingNotificationService _notificationService;

    public ConfirmModel(BookingRepository bookingRepository, BookingNotificationService notificationService)
    {
        _bookingRepository = bookingRepository;
        _notificationService = notificationService;
    }

    public BookingFormRecord? Booking { get; set; }
    public string ResultMessage { get; set; } = "";
    public bool ResultIsError { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid newId)
    {
        var booking = await _bookingRepository.GetByNewIdAsync(newId);
        if (booking is null)
        {
            return NotFound();
        }

        switch (booking.Status)
        {
            case "Approved":
                Booking = await _bookingRepository.ConfirmAsync(newId);
                await _notificationService.NotifyManagementClientConfirmedAsync(Booking);
                ResultMessage = "Your reservation is confirmed. We look forward to hosting you!";
                break;

            case "Confirmed":
                Booking = booking;
                ResultMessage = "This reservation was already confirmed. See you soon!";
                break;

            default:
                Booking = booking;
                ResultIsError = true;
                ResultMessage = booking.Status == "Disapproved"
                    ? "This booking request was not approved, so it can't be confirmed."
                    : "This booking hasn't been approved yet, so it can't be confirmed.";
                break;
        }

        return Page();
    }
}
