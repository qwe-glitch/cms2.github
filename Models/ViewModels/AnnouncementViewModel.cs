using System;

namespace ComplaintManagementSystem.Models.ViewModels;

public class AnnouncementViewModel
{
    public int Id { get; set; }

    // Initialize with empty string to prevent CS8618 warning
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;

    // Image is optional, so marked as nullable (string?)
    public string? ImageUrl { get; set; }

    public DateTime PostedDate { get; set; }

    public string Category { get; set; } = "General";

    public bool IsPinned { get; set; }



}