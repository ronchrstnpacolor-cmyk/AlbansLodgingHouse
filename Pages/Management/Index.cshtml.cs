using AlbansLodgingHouse.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlbansLodgingHouse.Pages.Management;

public class IndexModel : PageModel
{
    private readonly BookingRepository _bookingRepository;
    private readonly RoomTypeRepository _roomTypeRepository;
    private readonly ManagementAccessService _accessService;

    public IndexModel(
        BookingRepository bookingRepository,
        RoomTypeRepository roomTypeRepository,
        ManagementAccessService accessService)
    {
        _bookingRepository = bookingRepository;
        _roomTypeRepository = roomTypeRepository;
        _accessService = accessService;
    }

    public int NewBookingsCount { get; set; }
    public int ActiveRoomTypesCount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await _accessService.IsHostAllowedAsync(Request.Host.Host)) return NotFound();

        var newBookings = await _bookingRepository.GetNewBookingsAsync();
        var roomTypes = await _roomTypeRepository.GetAllAsync();

        NewBookingsCount = newBookings.Count;
        ActiveRoomTypesCount = roomTypes.Count(r => r.IsActive);

        return Page();
    }
}
