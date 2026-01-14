using System.ComponentModel.DataAnnotations;

namespace ComplaintManagementSystem.Models.ViewModels;

/// <summary>
/// ViewModel for user login
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string UserType { get; set; } = "Citizen"; // Optional - auto-detected from database

    public bool RememberMe { get; set; }
}
