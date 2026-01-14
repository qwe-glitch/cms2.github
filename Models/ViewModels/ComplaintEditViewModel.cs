using System.ComponentModel.DataAnnotations;

namespace ComplaintManagementSystem.Models.ViewModels;

/// <summary>
/// ViewModel for editing an existing complaint (by Citizen)
/// </summary>
public class ComplaintEditViewModel
{
    public int ComplaintId { get; set; }

    [Required(ErrorMessage = "Please enter a title for your complaint")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please provide a description")]
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please specify the location")]
    [StringLength(300, ErrorMessage = "Location cannot exceed 300 characters")]
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a category")]
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Please select priority level")]
    public string Priority { get; set; } = "Medium"; // Low, Medium, High

    public bool IsAnonymous { get; set; } = false;

    public string? ContactPreference { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // For file uploads (optional new attachments)
    public List<IFormFile>? Attachments { get; set; }
    
    // To display existing attachments
    public List<ExistingAttachmentViewModel>? ExistingAttachments { get; set; }

    // IDs of attachments to delete
    public List<int>? AttachmentsToDelete { get; set; }
}

public class ExistingAttachmentViewModel
{
    public int AttachmentId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? FileType { get; set; }
}
