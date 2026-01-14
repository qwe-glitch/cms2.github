using System.ComponentModel.DataAnnotations;

namespace ComplaintManagementSystem.Models.ViewModels;

/// <summary>
/// ViewModel for submitting feedback on a complaint
/// </summary>
public class FeedbackViewModel
{
    public int ComplaintId { get; set; }

    [Required(ErrorMessage = "Please provide a rating")]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    [Required(ErrorMessage = "Please provide your feedback")]
    [StringLength(1000, ErrorMessage = "Feedback cannot exceed 1000 characters")]
    public string Comment { get; set; } = string.Empty;
}
