using ComplaintManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintManagementSystem.Controllers;

/// <summary>
/// Handles staff-specific operations
/// </summary>
public class StaffController : Controller
{
    private readonly IComplaintService _complaintService;

    public StaffController(IComplaintService complaintService)
    {
        _complaintService = complaintService;
    }

    // GET: /Staff/MyWork - Staff work dashboard
    public async Task<IActionResult> MyWork(string? tab = null)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null || userType != "Staff")
        {
            return RedirectToAction("Login", "Account");
        }

        // Get resolved, in-progress, and pending complaints
        var resolvedComplaints = await _complaintService.GetStaffResolvedComplaintsAsync(userId.Value);
        var inProgressComplaints = await _complaintService.GetStaffInProgressComplaintsAsync(userId.Value);
        var pendingComplaints = await _complaintService.GetStaffPendingComplaintsAsync(userId.Value);

        ViewBag.ResolvedComplaints = resolvedComplaints;
        ViewBag.InProgressComplaints = inProgressComplaints;
        ViewBag.PendingComplaints = pendingComplaints;
        ViewBag.ResolvedCount = resolvedComplaints.Count();
        ViewBag.InProgressCount = inProgressComplaints.Count();
        ViewBag.PendingCount = pendingComplaints.Count();
        ViewBag.ActiveTab = tab ?? "inprogress";
        ViewBag.UserType = userType;

        return View();
    }
}
