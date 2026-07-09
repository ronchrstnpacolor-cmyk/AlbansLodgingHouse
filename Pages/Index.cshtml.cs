using AlbansLodgingHouse.Data;
using AlbansLodgingHouse.Models;
using AlbansLodgingHouse.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlbansLodgingHouse.Pages;

public record GalleryImage(string Src, string Caption);

public record Testimonial(string Quote, string Author);

public record RoomListing(
    string Category,
    string Badge,
    string[] Images,
    string Title,
    string Description,
    string[] Features,
    string Price);

public record WhyItem(string Number, string Title, string Description);

public record ChecklistItem(string Text);

public record BreakfastItem(string Title, string Subtitle);

public record OfferTile(string Percent, string Description);

public record ReservationContact(string Carrier, string Number, string TelHref);

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly BookingRepository _bookingRepository;
    private readonly QrCodeService _qrCodeService;
    private readonly BookingNotificationService _notificationService;
    private readonly RoomTypeRepository _roomTypeRepository;
    private readonly RoomTypeImageRepository _roomTypeImageRepository;

    public IndexModel(
        ILogger<IndexModel> logger,
        BookingRepository bookingRepository,
        QrCodeService qrCodeService,
        BookingNotificationService notificationService,
        RoomTypeRepository roomTypeRepository,
        RoomTypeImageRepository roomTypeImageRepository)
    {
        _logger = logger;
        _bookingRepository = bookingRepository;
        _qrCodeService = qrCodeService;
        _notificationService = notificationService;
        _roomTypeRepository = roomTypeRepository;
        _roomTypeImageRepository = roomTypeImageRepository;
    }

    private IReadOnlyDictionary<Guid, List<RoomTypeImage>> _roomImages = new Dictionary<Guid, List<RoomTypeImage>>();

    public IReadOnlyList<RoomType> ActiveRoomTypes { get; set; } = Array.Empty<RoomType>();

    [BindProperty]
    public ReservationRequest Reservation { get; set; } = new();

    public IReadOnlyList<GalleryImage> Gallery { get; } = new[]
    {
        new GalleryImage("img/exterior.jpg", "Alban's Lodging House"),
        new GalleryImage("img/living.jpg", "Living & dining lounge"),
        new GalleryImage("img/kitchen.jpg", "Shared kitchen"),
        new GalleryImage("img/room-twin.jpg", "Private twin room"),
        new GalleryImage("img/dorm-red.jpg", "Transient dormitory"),
        new GalleryImage("img/dorm-green.jpg", "Ladies dormitory"),
        new GalleryImage("img/corridor.jpg", "Hallway lounge"),
        new GalleryImage("img/reception.jpg", "Front desk"),
    };

    public IReadOnlyList<Testimonial> Testimonials { get; } = new[]
    {
        new Testimonial(
            "Spotless rooms and a genuinely helpful front desk. I stayed a week for work and everything — wifi, aircon, the coffee — just worked.",
            "Marco D. — Business Traveler"),
        new Testimonial(
            "The ladies' dormitory felt safe and private. Having my own locker with a key made a real difference on a solo trip.",
            "Angela R. — Solo Traveler"),
        new Testimonial(
            "Great value and walking distance to the university and town center. Clean kitchen, quiet at night. I'll definitely book again.",
            "Jomar T. — Returning Guest"),
    };

    public IReadOnlyList<RoomListing> Rooms { get; } = new[]
    {
        new RoomListing(
            "private",
            "Private",
            new[] { "img/room-twin.jpg" },
            "Private Twin / Double Room",
            "A private room with two beds, air-conditioning and warm furnishings — ideal for business travelers or couples.",
            new[] { "2 beds", "Aircon", "Private" },
            "₱1,000 – ₱2,500 / night"),
        new RoomListing(
            "ladies",
            "Ladies Only",
            new[] { "img/dorm-green.jpg" },
            "Ladies' Dormitory",
            "A women-only dormitory that accommodates 5 bed spacers per room, each with an individual locker and key. Refrigerator and gas range in the kitchen, cable TV in the living room.",
            new[] { "Women only", "5 bed spacers", "Locker + key", "Kitchen" },
            "₱1,000 / month"),
    };

    public IReadOnlyList<RoomListing> DisplayRooms =>
        ActiveRoomTypes.Count > 0 ? ActiveRoomTypes.Select(ToRoomListing).ToList() : Rooms;

    private RoomListing ToRoomListing(RoomType room)
    {
        var isLadies = room.RoomTypeName.Contains("ladies", StringComparison.OrdinalIgnoreCase)
            || room.RoomTypeName.Contains("dorm", StringComparison.OrdinalIgnoreCase);

        var category = isLadies ? "ladies" : "private";
        var badge = isLadies ? "Ladies Only" : "Private";
        var fallbackImage = isLadies ? "img/dorm-green.jpg" : "img/room-twin.jpg";

        var images = _roomImages.TryGetValue(room.NewID, out var uploaded) && uploaded.Count > 0
            ? uploaded.Select(i => i.ImagePath).ToArray()
            : new[] { fallbackImage };

        var features = new List<string>
        {
            room.Beds == 1 ? "1 bed" : $"{room.Beds} beds",
            room.Pax == 1 ? "1 guest" : $"Up to {room.Pax} guests",
            room.Term,
        };
        if (room.Discount > 0)
        {
            features.Add($"{room.Discount:N0}% off");
        }

        var description = !string.IsNullOrWhiteSpace(room.Description)
            ? room.Description
            : isLadies
                ? $"A women-only dormitory with {room.Beds} bed spacer{(room.Beds == 1 ? "" : "s")}, individual lockers, and access to the shared kitchen and cable-TV living room."
                : $"A private room with {room.Beds} bed{(room.Beds == 1 ? "" : "s")}, air-conditioning and warm furnishings — accommodates up to {room.Pax} guest{(room.Pax == 1 ? "" : "s")}.";

        var price = $"₱{room.PriceRate:N0} / {room.Term}";

        return new RoomListing(category, badge, images, room.RoomTypeName, description, features.ToArray(), price);
    }

    public IReadOnlyList<WhyItem> WhyChooseUs { get; } = new[]
    {
        new WhyItem("01", "Clean, nicely decorated rooms", "Every room is kept spotless and thoughtfully furnished."),
        new WhyItem("02", "Free WiFi & internet", "Free WiFi throughout, plus computer internet access."),
        new WhyItem("03", "Friendly, attentive staff", "A team ready to serve your needs, any hour of the day."),
        new WhyItem("04", "Affordable, reasonable rates", "Fair pricing for business, transient and long stays."),
        new WhyItem("05", "24-hour desk & security", "A receptionist and security guard on duty around the clock."),
        new WhyItem("06", "Free complimentary breakfast", "A hot breakfast included with every stay."),
    };

    public IReadOnlyList<ChecklistItem> LodgingHouseFeatures { get; } = new[]
    {
        new ChecklistItem("Flat-screen cable TV"),
        new ChecklistItem("Air-conditioned rooms"),
        new ChecklistItem("Hot water in every room"),
        new ChecklistItem("Small grocery store"),
        new ChecklistItem("Small gift shop"),
        new ChecklistItem("Free parking"),
    };

    public IReadOnlyList<ChecklistItem> GuestServices { get; } = new[]
    {
        new ChecklistItem("Free computer access"),
        new ChecklistItem("24-hour receptionist on duty"),
        new ChecklistItem("24-hour security guard"),
        new ChecklistItem("Bathrobes & slippers provided"),
        new ChecklistItem("Ice-cold bottled water"),
        new ChecklistItem("Complimentary toiletries"),
    };

    public IReadOnlyList<BreakfastItem> BreakfastMenu { get; } = new[]
    {
        new BreakfastItem("Fried ham & egg", "with rice & coffee"),
        new BreakfastItem("Ham & egg omelette", "with rice & coffee"),
        new BreakfastItem("Corned beef omelette", "with rice & coffee"),
        new BreakfastItem("Tocino & egg", "with rice & coffee"),
        new BreakfastItem("Longganisa & egg", "with rice & coffee"),
        new BreakfastItem("Fried bangus & egg", "with rice & coffee"),
    };

    public IReadOnlyList<OfferTile> Offers { get; } = new[]
    {
        new OfferTile("10%", "for regular customers"),
        new OfferTile("10%", "if you stay for 1 week"),
        new OfferTile("20%", "if you stay for 2 weeks"),
        new OfferTile("10%", "if you rent 4 rooms"),
        new OfferTile("20%", "if you rent 8 rooms"),
        new OfferTile("10%", "off with a promo card"),
    };

    public IReadOnlyList<ChecklistItem> NearbyLandmarks { get; } = new[]
    {
        new ChecklistItem("Agoo Municipal Hall"),
        new ChecklistItem("Agoo Basilica Church"),
        new ChecklistItem("DMMSU-SLUC University"),
        new ChecklistItem("Metro Bank & Land Bank"),
        new ChecklistItem("PNB, BPI & Banco de Oro"),
        new ChecklistItem("Rural Bank & Rang-ay Bank"),
    };

    public IReadOnlyList<ReservationContact> ReservationContacts { get; } = new[]
    {
        new ReservationContact("PLDT", "72-607-2696", "tel:726072696"),
        new ReservationContact("Smart", "929-845-1276", "tel:+639298451276"),
        new ReservationContact("Globe", "927-289-0449", "tel:+639272890449"),
    };

    public async Task OnGetAsync()
    {
        var activeRoomTypes = await _roomTypeRepository.GetActiveAsync();
        ActiveRoomTypes = activeRoomTypes.Where(r => r.RoomsAvailable > 0).ToList();
        _roomImages = await _roomTypeImageRepository.GetForRoomTypesAsync(ActiveRoomTypes.Select(r => r.NewID));
    }

    public async Task<IActionResult> OnPostReservation()
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(kvp => kvp.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray());

            return BadRequest(new { ok = false, errors });
        }

        var booking = await _bookingRepository.InsertBookingAsync(
            Reservation.Name,
            Reservation.Phone,
            Reservation.Email,
            Reservation.CheckIn,
            Reservation.CheckOut,
            Reservation.Room,
            Reservation.Guests,
            Reservation.Message);

        _logger.LogInformation(
            "Reservation {BookingReferenceNo} received from {Name} ({Contact}) for {Room}, {CheckIn:yyyy-MM-dd} to {CheckOut:yyyy-MM-dd}, {Guests} guest(s).",
            booking.BookingReferenceNo,
            Reservation.Name,
            string.IsNullOrWhiteSpace(Reservation.Email) ? Reservation.Phone : Reservation.Email,
            string.IsNullOrWhiteSpace(Reservation.Room) ? "unspecified room" : Reservation.Room,
            Reservation.CheckIn,
            Reservation.CheckOut,
            Reservation.Guests);

        var managementUrl = Url.Page("/Management/Bookings", null, null, Request.Scheme, Request.Host.Value) ?? "";
        await _notificationService.NotifyManagementNewBookingAsync(booking, managementUrl);

        var qrText = QrCodeService.BuildBookingQrText(booking);

        return new JsonResult(new
        {
            ok = true,
            message = "Thank you — request received. We'll confirm your stay shortly.",
            bookingReferenceNo = booking.BookingReferenceNo,
            newId = booking.NewID,
            fullName = booking.FullName,
            checkIn = booking.CheckIn?.ToString("yyyy-MM-dd"),
            checkOut = booking.CheckOut?.ToString("yyyy-MM-dd"),
            roomType = booking.RoomType,
            pax = booking.Pax,
            qrCodeDataUri = _qrCodeService.GeneratePngDataUri(qrText),
        });
    }
}
