using System.ComponentModel.DataAnnotations;

namespace ComplaintManagementSystem.Models.ViewModels;

/// <summary>
/// ViewModel for updating an existing complaint (by Staff/Admin)
/// </summary>
public class ComplaintUpdateViewModel
{
    public int ComplaintId { get; set; }

    [Required(ErrorMessage = "Please select a status")]
    public string Status { get; set; } = string.Empty; // Pending, In Progress, Resolved, Closed, Rejected

    public int? AssignedStaffId { get; set; }

    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string? UpdateNotes { get; set; }

    public string? Priority { get; set; } // Low, Medium, High

    public List<IFormFile>? Attachments { get; set; } // Optional files to upload
}
