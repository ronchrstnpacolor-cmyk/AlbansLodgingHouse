using System.ComponentModel.DataAnnotations;

namespace AlbansLodgingHouse.Models;

public class ReservationRequest : IValidatableObject
{
    [Required(ErrorMessage = "Please tell us your name.")]
    [StringLength(120)]
    public string Name { get; set; } = "";

    [EmailAddress(ErrorMessage = "That email doesn't look right.")]
    [StringLength(160)]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "That phone number doesn't look right.")]
    [StringLength(40)]
    public string? Phone { get; set; }

    public DateOnly? CheckIn { get; set; }

    public DateOnly? CheckOut { get; set; }

    [StringLength(80)]
    public string? Room { get; set; }

    [Range(1, 20, ErrorMessage = "Guests must be between 1 and 20.")]
    public int Guests { get; set; } = 2;

    [StringLength(1000)]
    public string? Message { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CheckIn is not null && CheckOut is not null && CheckOut <= CheckIn)
        {
            yield return new ValidationResult(
                "Check-out must be after check-in.",
                new[] { nameof(CheckOut) });
        }
    }
}
