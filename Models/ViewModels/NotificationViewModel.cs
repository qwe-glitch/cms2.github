namespace ComplaintManagementSystem.Models.ViewModels;

/// <summary>
/// ViewModel for displaying notifications
/// </summary>
public class NotificationViewModel
{
    public int NotificationId { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime SentAt { get; set; }
    public int? ComplaintId { get; set; }
    public string? ComplaintTitle { get; set; }
}
