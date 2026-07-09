namespace AlbansLodgingHouse.Models;

public class RoomTypeImage
{
    public int RecordNo { get; set; }
    public Guid NewID { get; set; }
    public Guid RoomTypeNewID { get; set; }
    public string ImagePath { get; set; } = "";
    public int SortOrder { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime DateCreated { get; set; }
}
