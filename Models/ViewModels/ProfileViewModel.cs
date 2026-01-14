using System.ComponentModel.DataAnnotations;

namespace ComplaintManagementSystem.Models.ViewModels;

/// <summary>
/// ViewModel for citizen profile management
/// </summary>
public class ProfileViewModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(100)]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    [Phone(ErrorMessage = "Invalid phone number")]
    [RegularExpression(@"^01\d{1}[-\s]?\d{7,8}$", 
        ErrorMessage = "Phone must be a valid Malaysian mobile number (e.g., 012-3456789)")]
    public string? Phone { get; set; }

    public string? Address { get; set; }

    // Optional password change fields
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
        ErrorMessage = "Password must have at least 8 characters, one uppercase, one lowercase, one number and one special character.")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string? ConfirmPassword { get; set; }
}
