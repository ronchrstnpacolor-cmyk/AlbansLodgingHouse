namespace AlbansLodgingHouse.Models;

public class RoomType
{
    public int RecordNo { get; set; }
    public Guid NewID { get; set; }
    public string RoomTypeName { get; set; } = "";
    public string? Description { get; set; }
    public decimal PriceRate { get; set; }
    public string Term { get; set; } = "";
    public int Beds { get; set; }
    public int Pax { get; set; }
    public int RoomsAvailable { get; set; }
    public DateOnly? DateAvailable { get; set; }
    public decimal Discount { get; set; }
    public bool IsActive { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime DateCreated { get; set; }
    public string? ModifiedBy { get; set; }
    public DateTime? DateModified { get; set; }
}
