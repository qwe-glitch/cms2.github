using ComplaintManagementSystem.Models.ViewModels;

namespace ComplaintManagementSystem.Services;

/// <summary>
/// Interface for notification-related operations
/// </summary>
public interface INotificationService
{
    // DFD 5.0 - Send Notification
    Task SendNotificationAsync(string message, int complaintId, int? citizenId, int? staffId, int? adminId);

    // DFD 6.0 - Get Notifications
    Task<IEnumerable<NotificationViewModel>> GetNotificationsByCitizenAsync(int citizenId);
    Task<IEnumerable<NotificationViewModel>> GetNotificationsByStaffAsync(int staffId);
    Task<IEnumerable<NotificationViewModel>> GetNotificationsByAdminAsync();

    // Helper methods
    Task<bool> MarkAsReadAsync(int notificationId);
    Task<int> MarkAllAsReadAsync(int? citizenId, int? staffId, int? adminId);
    Task<int> GetUnreadCountAsync(int? citizenId, int? staffId, int? adminId);
    Task<bool> DeleteNotificationAsync(int notificationId);
}
