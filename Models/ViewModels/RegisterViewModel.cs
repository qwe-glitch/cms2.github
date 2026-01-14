using System.ComponentModel.DataAnnotations;

namespace ComplaintManagementSystem.Models.ViewModels;

/// <summary>
/// ViewModel for citizen registration
/// </summary>
public class RegisterViewModel
{
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm your password")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Invalid phone number")]
    [RegularExpression(@"^01\d{1}[-\s]?\d{7,8}$", 
        ErrorMessage = "Phone must be a valid Malaysian mobile number (e.g., 012-3456789)")]
    public string? Phone { get; set; }

    [StringLength(300, ErrorMessage = "Address cannot exceed 300 characters")]
    public string? Address { get; set; }
}
