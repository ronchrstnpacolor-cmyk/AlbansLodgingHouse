using AlbansLodgingHouse.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlbansLodgingHouse.Pages;

public record GalleryImage(string Src, string Caption);

public record Testimonial(string Quote, string Author);

public record RoomListing(
    string Category,
    string Badge,
    string Image,
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

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

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
            "img/room-twin.jpg",
            "Private Twin / Double Room",
            "A private room with two beds, air-conditioning and warm furnishings — ideal for business travelers or couples.",
            new[] { "2 beds", "Aircon", "Private" },
            "₱1,000 – ₱2,500 / night"),
        new RoomListing(
            "ladies",
            "Ladies Only",
            "img/dorm-green.jpg",
            "Ladies' Dormitory",
            "A women-only dormitory that accommodates 5 bed spacers per room, each with an individual locker and key. Refrigerator and gas range in the kitchen, cable TV in the living room.",
            new[] { "Women only", "5 bed spacers", "Locker + key", "Kitchen" },
            "₱1,000 / month"),
    };

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

    public void OnGet()
    {
    }

    public IActionResult OnPostReservation()
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

        _logger.LogInformation(
            "Reservation request received from {Name} ({Contact}) for {Room}, {CheckIn:yyyy-MM-dd} to {CheckOut:yyyy-MM-dd}, {Guests} guest(s).",
            Reservation.Name,
            string.IsNullOrWhiteSpace(Reservation.Email) ? Reservation.Phone : Reservation.Email,
            string.IsNullOrWhiteSpace(Reservation.Room) ? "unspecified room" : Reservation.Room,
            Reservation.CheckIn,
            Reservation.CheckOut,
            Reservation.Guests);

        return new JsonResult(new
        {
            ok = true,
            message = "Thank you — request received. We'll confirm your stay shortly.",
        });
    }
}
