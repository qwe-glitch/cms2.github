using ComplaintManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintManagementSystem.Controllers;

/// <summary>
/// Handles notification operations
/// </summary>
public class NotificationController : Controller
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    // GET: /Notification/Index
    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        IEnumerable<Models.ViewModels.NotificationViewModel> notifications;

        if (userType == "Citizen")
        {
            notifications = await _notificationService.GetNotificationsByCitizenAsync(userId.Value);
        }
        else if (userType == "Staff")
        {
            notifications = await _notificationService.GetNotificationsByStaffAsync(userId.Value);
        }
        else if (userType == "Admin")
        {
            notifications = await _notificationService.GetNotificationsByAdminAsync();
        }
        else
        {
            notifications = new List<Models.ViewModels.NotificationViewModel>();
        }

        ViewBag.UserType = userType;
        return View(notifications);
    }

    // POST: /Notification/MarkAsRead
    [HttpPost]
    public async Task<IActionResult> MarkAsRead([FromBody] MarkAsReadRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return Json(new { success = false, message = "Unauthorized" });
        }

        var success = await _notificationService.MarkAsReadAsync(request.NotificationId);
        return Json(new { success = success });
    }

    public class MarkAsReadRequest
    {
        public int NotificationId { get; set; }
    }

    // POST: /Notification/MarkAllAsRead
    [HttpPost]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null)
        {
            return Json(new { success = false, message = "Unauthorized" });
        }

        int count = 0;

        if (userType == "Citizen")
        {
            count = await _notificationService.MarkAllAsReadAsync(userId.Value, null, null);
        }
        else if (userType == "Staff")
        {
            count = await _notificationService.MarkAllAsReadAsync(null, userId.Value, null);
        }
        else if (userType == "Admin")
        {
            count = await _notificationService.MarkAllAsReadAsync(null, null, userId.Value);
        }

        return Json(new { success = true, count = count });
    }

    // GET: /Notification/UnreadCount (AJAX)
    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null)
        {
            return Json(new { count = 0 });
        }

        int count = 0;

        if (userType == "Citizen")
        {
            count = await _notificationService.GetUnreadCountAsync(userId.Value, null, null);
        }
        else if (userType == "Staff")
        {
            count = await _notificationService.GetUnreadCountAsync(null, userId.Value, null);
        }
        else if (userType == "Admin")
        {
            count = await _notificationService.GetUnreadCountAsync(null, null, userId.Value);
        }

        return Json(new { count = count });
    }

    // POST: /Notification/Delete
    [HttpPost]
    public async Task<IActionResult> Delete([FromBody] DeleteNotificationRequest request)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return Json(new { success = false, message = "Unauthorized" });
        }

        var success = await _notificationService.DeleteNotificationAsync(request.NotificationId);
        return Json(new { success = success });
    }

    public class DeleteNotificationRequest
    {
        public int NotificationId { get; set; }
    }
}
