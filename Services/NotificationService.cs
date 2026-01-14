using ComplaintManagementSystem.Models;
using ComplaintManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagementSystem.Services;

/// <summary>
/// Implementation of notification-related operations
/// </summary>
public class NotificationService : INotificationService
{
    private readonly DB _context;

    public NotificationService(DB context)
    {
        _context = context;
    }

    // DFD 5.0 - Send Notification
    public async Task SendNotificationAsync(string message, int complaintId, int? citizenId, int? staffId, int? adminId)
    {
        var notification = new Notification
        {
            Message = message,
            IsRead = false,
            SentAt = DateTime.Now,
            ComplaintId = complaintId,
            CitizenId = citizenId,
            StaffId = staffId,
            AdminId = adminId
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
    }

    // DFD 6.0 - Get Notifications (for Citizens)
    public async Task<IEnumerable<NotificationViewModel>> GetNotificationsByCitizenAsync(int citizenId)
    {
        return await _context.Notifications
            .Where(n => n.CitizenId == citizenId)
            .Include(n => n.Complaint)
            .OrderByDescending(n => n.SentAt)
            .Select(n => new NotificationViewModel
            {
                NotificationId = n.NotificationId,
                Message = n.Message,
                IsRead = n.IsRead,
                SentAt = n.SentAt,
                ComplaintId = n.ComplaintId,
                ComplaintTitle = n.Complaint != null ? n.Complaint.Title : null
            })
            .ToListAsync();
    }

    // DFD 6.0 - Get Notifications (for Staff)
    public async Task<IEnumerable<NotificationViewModel>> GetNotificationsByStaffAsync(int staffId)
    {
        return await _context.Notifications
            .Where(n => n.StaffId == staffId)
            .Include(n => n.Complaint)
            .OrderByDescending(n => n.SentAt)
            .Select(n => new NotificationViewModel
            {
                NotificationId = n.NotificationId,
                Message = n.Message,
                IsRead = n.IsRead,
                SentAt = n.SentAt,
                ComplaintId = n.ComplaintId,
                ComplaintTitle = n.Complaint != null ? n.Complaint.Title : null
            })
            .ToListAsync();
    }

    // DFD 6.0 - Get Notifications (for Admin)
    public async Task<IEnumerable<NotificationViewModel>> GetNotificationsByAdminAsync()
    {
        return await _context.Notifications
            .Where(n => n.AdminId != null)
            .Include(n => n.Complaint)
            .OrderByDescending(n => n.SentAt)
            .Select(n => new NotificationViewModel
            {
                NotificationId = n.NotificationId,
                Message = n.Message,
                IsRead = n.IsRead,
                SentAt = n.SentAt,
                ComplaintId = n.ComplaintId,
                ComplaintTitle = n.Complaint != null ? n.Complaint.Title : null
            })
            .ToListAsync();
    }

    // Helper: Mark notification as read
    public async Task<bool> MarkAsReadAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification == null) return false;

        notification.IsRead = true;
        await _context.SaveChangesAsync();
        return true;
    }

    // Helper: Mark all notifications as read for a user
    public async Task<int> MarkAllAsReadAsync(int? citizenId, int? staffId, int? adminId)
    {
        List<Notification> notifications;

        if (citizenId.HasValue)
        {
            notifications = await _context.Notifications
                .Where(n => n.CitizenId == citizenId && !n.IsRead)
                .ToListAsync();
        }
        else if (staffId.HasValue)
        {
            notifications = await _context.Notifications
                .Where(n => n.StaffId == staffId && !n.IsRead)
                .ToListAsync();
        }
        else if (adminId.HasValue)
        {
            notifications = await _context.Notifications
                .Where(n => n.AdminId == adminId && !n.IsRead)
                .ToListAsync();
        }
        else
        {
            return 0;
        }

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
        }

        await _context.SaveChangesAsync();
        return notifications.Count;
    }

    // Helper: Get unread count
    public async Task<int> GetUnreadCountAsync(int? citizenId, int? staffId, int? adminId)
    {
        if (citizenId.HasValue)
        {
            return await _context.Notifications
                .Where(n => n.CitizenId == citizenId && !n.IsRead)
                .CountAsync();
        }
        else if (staffId.HasValue)
        {
            return await _context.Notifications
                .Where(n => n.StaffId == staffId && !n.IsRead)
                .CountAsync();
        }
        else if (adminId.HasValue)
        {
            return await _context.Notifications
                .Where(n => n.AdminId == adminId && !n.IsRead)
                .CountAsync();
        }

        return 0;
    }

    // Helper: Delete notification
    public async Task<bool> DeleteNotificationAsync(int notificationId)
    {
        var notification = await _context.Notifications.FindAsync(notificationId);
        if (notification == null) return false;

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();
        return true;
    }
}
