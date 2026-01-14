using ComplaintManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagementSystem.Services;

public class ComplaintReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ComplaintReminderService> _logger;
    private const int CheckIntervalHours = 24;
    private const int OverdueDays = 14;
    private const int ReminderFrequencyDays = 7; // Remind every week after the first one

    public ComplaintReminderService(IServiceProvider serviceProvider, ILogger<ComplaintReminderService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Complaint Reminder Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndSendRemindersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking for overdue complaints.");
            }

            // Wait for 24 hours before next check
            // For testing purposes, this can be adjusted
            await Task.Delay(TimeSpan.FromHours(CheckIntervalHours), stoppingToken);
        }
    }

    private async Task CheckAndSendRemindersAsync()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<DB>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var cutoffDate = DateTime.UtcNow.AddDays(-OverdueDays);

            // Find complaints that are older than 2 weeks and not resolved/closed
            // Also ensure they have an assigned staff member
            var overdueComplaints = await context.Complaints
                .Where(c => c.SubmittedAt < cutoffDate 
                            && c.Status != "Resolved" 
                            && c.Status != "Closed"
                            && c.StaffId != null)
                .Include(c => c.Notifications)
                .ToListAsync();

            foreach (var complaint in overdueComplaints)
            {
                // Check if we need to escalate priority
                if (complaint.Priority != "High")
                {
                    var oldPriority = complaint.Priority;
                    complaint.Priority = "High";
                    complaint.UpdatedAt = DateTime.UtcNow;

                    // Add history record
                    var history = new ComplaintHistory
                    {
                        ComplaintId = complaint.ComplaintId,
                        StatusBefore = complaint.Status,
                        StatusAfter = complaint.Status,
                        ChangeBy = "System (Auto-Escalation)",
                        ChangeTime = DateTime.UtcNow,
                        Notes = $"Priority auto-escalated from {oldPriority} to High due to being pending for over {OverdueDays} days."
                    };
                    context.ComplaintHistories.Add(history);
                    
                    await context.SaveChangesAsync();
                    _logger.LogInformation($"Auto-escalated priority for Complaint ID {complaint.ComplaintId} to High.");
                }

                // Check if we recently sent a reminder to avoid spamming
                // We look for notifications with our specific message pattern sent in the last ReminderFrequencyDays
                var lastReminder = complaint.Notifications
                    .Where(n => n.Message.StartsWith("Reminder: This complaint has been pending") 
                                && n.StaffId == complaint.StaffId)
                    .OrderByDescending(n => n.SentAt)
                    .FirstOrDefault();

                if (lastReminder != null && lastReminder.SentAt > DateTime.UtcNow.AddDays(-ReminderFrequencyDays))
                {
                    // Reminder sent recently, skip
                    continue;
                }

                // Send notification
                var message = $"Reminder: This complaint has been pending for over {OverdueDays} days. Priority has been escalated to High. Please take action.";
                await notificationService.SendNotificationAsync(message, complaint.ComplaintId, null, complaint.StaffId, null);
                
                _logger.LogInformation($"Sent reminder for Complaint ID {complaint.ComplaintId} to Staff ID {complaint.StaffId}");
            }
        }
    }
}
