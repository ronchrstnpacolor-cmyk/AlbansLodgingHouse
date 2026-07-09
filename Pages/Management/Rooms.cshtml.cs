using System.ComponentModel.DataAnnotations;
using AlbansLodgingHouse.Data;
using AlbansLodgingHouse.Models;
using AlbansLodgingHouse.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AlbansLodgingHouse.Pages.Management;

public class RoomTypeInput
{
    [Required(ErrorMessage = "Please enter a room type.")]
    [StringLength(80)]
    public string RoomType { get; set; } = "";

    [StringLength(500, ErrorMessage = "Description must be 500 characters or fewer.")]
    public string? Description { get; set; }

    [Range(0, 1000000, ErrorMessage = "Enter a valid price rate.")]
    public decimal PriceRate { get; set; }

    [Required]
    public string Term { get; set; } = "Short Term";

    [Range(1, 50, ErrorMessage = "Beds must be at least 1.")]
    public int Beds { get; set; } = 1;

    [Range(1, 50, ErrorMessage = "Pax must be at least 1.")]
    public int Pax { get; set; } = 1;

    [Range(0, 999, ErrorMessage = "Enter how many rooms of this type are available.")]
    public int RoomsAvailable { get; set; } = 1;

    public DateOnly? DateAvailable { get; set; }

    [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100.")]
    public decimal Discount { get; set; }

    public List<IFormFile> Photos { get; set; } = new();
}

public class RoomTypeEditInput
{
    [Required]
    public Guid NewID { get; set; }

    [Required(ErrorMessage = "Please enter a room type.")]
    [StringLength(80)]
    public string RoomType { get; set; } = "";

    [StringLength(500, ErrorMessage = "Description must be 500 characters or fewer.")]
    public string? Description { get; set; }

    [Range(0, 1000000, ErrorMessage = "Enter a valid price rate.")]
    public decimal PriceRate { get; set; }

    [Required]
    public string Term { get; set; } = "Short Term";

    [Range(1, 50, ErrorMessage = "Beds must be at least 1.")]
    public int Beds { get; set; } = 1;

    [Range(1, 50, ErrorMessage = "Pax must be at least 1.")]
    public int Pax { get; set; } = 1;

    [Range(0, 999, ErrorMessage = "Enter how many rooms of this type are available.")]
    public int RoomsAvailable { get; set; } = 1;

    public DateOnly? DateAvailable { get; set; }

    [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100.")]
    public decimal Discount { get; set; }
}

public class RoomsModel : PageModel
{
    private readonly RoomTypeRepository _roomTypeRepository;
    private readonly RoomTypeImageRepository _roomTypeImageRepository;
    private readonly RoomImageStorageService _imageStorage;
    private readonly ManagementAccessService _accessService;

    public RoomsModel(
        RoomTypeRepository roomTypeRepository,
        RoomTypeImageRepository roomTypeImageRepository,
        RoomImageStorageService imageStorage,
        ManagementAccessService accessService)
    {
        _roomTypeRepository = roomTypeRepository;
        _roomTypeImageRepository = roomTypeImageRepository;
        _imageStorage = imageStorage;
        _accessService = accessService;
    }

    public IReadOnlyList<RoomType> RoomTypes { get; set; } = Array.Empty<RoomType>();

    public IReadOnlyDictionary<Guid, List<RoomTypeImage>> RoomImages { get; set; } =
        new Dictionary<Guid, List<RoomTypeImage>>();

    public int MaxPhotosPerRoomType => RoomImageStorageService.MaxPhotosPerRoomType;

    public IReadOnlyList<RoomTypeImage> ImagesFor(Guid roomTypeId) =>
        RoomImages.TryGetValue(roomTypeId, out var images) ? images : Array.Empty<RoomTypeImage>();

    [BindProperty]
    public RoomTypeInput NewRoom { get; set; } = new();

    [BindProperty]
    public RoomTypeEditInput EditRoom { get; set; } = new();

    public string? StatusMessage { get; set; }
    public bool StatusIsError { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!await IsAllowedAsync()) return NotFound();

        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAddAsync()
    {
        if (!await IsAllowedAsync()) return NotFound();

        ModelState.Clear();
        if (!TryValidateModel(NewRoom, nameof(NewRoom)))
        {
            StatusIsError = true;
            StatusMessage = "Please fix the errors below.";
            await LoadAsync();
            return Page();
        }

        if (!ValidatePhotos(NewRoom.Photos, 0, out var photoError))
        {
            StatusIsError = true;
            StatusMessage = photoError;
            await LoadAsync();
            return Page();
        }

        var newId = await _roomTypeRepository.InsertAsync(
            NewRoom.RoomType,
            NewRoom.Description,
            NewRoom.PriceRate,
            NewRoom.Term,
            NewRoom.Beds,
            NewRoom.Pax,
            NewRoom.RoomsAvailable,
            NewRoom.DateAvailable,
            NewRoom.Discount,
            Request.Host.Host);

        await SavePhotosAsync(newId, NewRoom.Photos, 0);

        StatusMessage = $"Added room type \"{NewRoom.RoomType}\".";
        ModelState.Clear();
        NewRoom = new();
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostEditAsync()
    {
        if (!await IsAllowedAsync()) return NotFound();

        ModelState.Clear();
        if (!TryValidateModel(EditRoom, nameof(EditRoom)))
        {
            StatusIsError = true;
            StatusMessage = "Please fix the errors below.";
            await LoadAsync();
            return Page();
        }

        await _roomTypeRepository.UpdateAsync(
            EditRoom.NewID,
            EditRoom.RoomType,
            EditRoom.Description,
            EditRoom.PriceRate,
            EditRoom.Term,
            EditRoom.Beds,
            EditRoom.Pax,
            EditRoom.RoomsAvailable,
            EditRoom.DateAvailable,
            EditRoom.Discount,
            Request.Host.Host);

        StatusMessage = $"Updated room type \"{EditRoom.RoomType}\".";
        ModelState.Clear();
        EditRoom = new();
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostToggleAsync(Guid newId, bool makeActive)
    {
        if (!await IsAllowedAsync()) return NotFound();

        await _roomTypeRepository.SetActiveAsync(newId, makeActive, Request.Host.Host);

        StatusMessage = makeActive ? "Room type reactivated." : "Room type deactivated.";
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUploadPhotosAsync(Guid roomTypeId, List<IFormFile> photos)
    {
        if (!await IsAllowedAsync()) return NotFound();

        if (photos.Count == 0)
        {
            StatusIsError = true;
            StatusMessage = "Please choose at least one photo.";
            await LoadAsync();
            return Page();
        }

        var existingCount = await _roomTypeImageRepository.CountForRoomTypeAsync(roomTypeId);
        if (!ValidatePhotos(photos, existingCount, out var photoError))
        {
            StatusIsError = true;
            StatusMessage = photoError;
            await LoadAsync();
            return Page();
        }

        await SavePhotosAsync(roomTypeId, photos, existingCount);

        StatusMessage = photos.Count == 1 ? "Photo added." : $"{photos.Count} photos added.";
        await LoadAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteImageAsync(Guid imageId)
    {
        if (!await IsAllowedAsync()) return NotFound();

        var imagePath = await _roomTypeImageRepository.DeleteAsync(imageId);
        if (imagePath is not null)
        {
            await _imageStorage.DeleteAsync(imagePath);
            StatusMessage = "Photo removed.";
        }

        await LoadAsync();
        return Page();
    }

    private bool ValidatePhotos(List<IFormFile> photos, int existingCount, out string? error)
    {
        if (existingCount + photos.Count > MaxPhotosPerRoomType)
        {
            error = existingCount > 0
                ? $"This room type already has {existingCount} photo(s); you can add up to {MaxPhotosPerRoomType - existingCount} more."
                : $"You can upload up to {MaxPhotosPerRoomType} photos.";
            return false;
        }

        foreach (var photo in photos)
        {
            if (!RoomImageStorageService.IsAllowed(photo, out error))
            {
                return false;
            }
        }

        error = null;
        return true;
    }

    private async Task SavePhotosAsync(Guid roomTypeId, List<IFormFile> photos, int startingSortOrder)
    {
        var sortOrder = startingSortOrder;
        foreach (var photo in photos)
        {
            var path = await _imageStorage.SaveAsync(roomTypeId, photo);
            await _roomTypeImageRepository.InsertAsync(roomTypeId, path, sortOrder++, Request.Host.Host);
        }
    }

    private async Task LoadAsync()
    {
        RoomTypes = await _roomTypeRepository.GetAllAsync();
        RoomImages = await _roomTypeImageRepository.GetForRoomTypesAsync(RoomTypes.Select(r => r.NewID));
    }

    private async Task<bool> IsAllowedAsync()
    {
        return await _accessService.IsHostAllowedAsync(Request.Host.Host);
    }
}
