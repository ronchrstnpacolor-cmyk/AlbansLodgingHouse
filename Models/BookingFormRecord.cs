namespace AlbansLodgingHouse.Models;

public class BookingFormRecord
{
    public int RecordNo { get; set; }
    public Guid NewID { get; set; }
    public string BookingReferenceNo { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? PhoneNo { get; set; }
    public string? Email { get; set; }
    public DateOnly? CheckIn { get; set; }
    public DateOnly? CheckOut { get; set; }
    public string? RoomType { get; set; }
    public int Pax { get; set; }
    public string? Message { get; set; }
    public string Status { get; set; } = "";
    public DateTime DateCreated { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? DateModified { get; set; }
}
