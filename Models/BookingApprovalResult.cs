namespace AlbansLodgingHouse.Models;

public class BookingApprovalResult
{
    public int RecordNo { get; set; }
    public Guid NewID { get; set; }
    public string BookingReferenceNo { get; set; } = "";
    public string Status { get; set; } = "";
    public string? Remarks { get; set; }
    public string CreatedBy { get; set; } = "";
}
