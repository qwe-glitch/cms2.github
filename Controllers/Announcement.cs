using Microsoft.AspNetCore.Mvc;
using ComplaintManagementSystem.Services;
using ComplaintManagementSystem.Models.ViewModels;
using System.Linq;

namespace ComplaintManagementSystem.Controllers;

public class AnnouncementController : Controller
{
    private readonly IComplaintService _complaintService;

    // ✅ [Added]: Static variable to simulate database storage for system announcements (resets on restart)
    private static AnnouncementViewModel _currentSystemAnnouncement = new AnnouncementViewModel
    {
        Id = 0,
        Title = "System Maintenance & Upgrades",
        Content = "We will be performing scheduled server maintenance on Sunday, 12:00 AM - 4:00 AM. The dashboard may be intermittent during this time.",
        PostedDate = DateTime.Now,
        Category = "System",
        IsPinned = true
    };

    public AnnouncementController(IComplaintService complaintService)
    {
        _complaintService = complaintService;
    }

    public async Task<IActionResult> Index()
    {
        var rawComplaints = await _complaintService.GetAllComplaintsAsync();

        // Filter to show only complaints from the last 1 week
        var oneWeekAgo = DateTime.Now.AddDays(-7);
        var recentComplaints = rawComplaints
            .Where(c => {
                dynamic d = c;
                DateTime submittedAt = d.SubmittedAt ?? DateTime.MinValue;
                return submittedAt >= oneWeekAgo;
            })
            .ToList();

        var announcements = recentComplaints
            .Select(c => {
                dynamic d = c;

                // ✅ 1. More robust status check (case-insensitive)
                string status = d.Status?.ToString() ?? "";
                bool isResolved = status.Trim().Equals("Resolved", StringComparison.OrdinalIgnoreCase);

                return new AnnouncementViewModel
                {
                    Id = d.ComplaintId,
                    Title = d.Title ?? "Untitled",
                    Content = isResolved
                        ? $"Good news! The issue '{d.Title}' reported on {d.SubmittedAt:MMM dd} has been successfully resolved."
                        : $"Work in progress: Our team is currently attending to '{d.Title}'. Status: {d.Status}",

                    PostedDate = d.UpdatedAt ?? DateTime.Now,
                    // ✅ 2. The Category here determines the color in the View
                    Category = isResolved ? "Success" : "In Progress",
                    IsPinned = isResolved,
                    ImageUrl = null
                };
            })
            // Ensure only these two categories are included
            .Where(x => x.Category == "Success" || x.Category == "In Progress")
            .OrderByDescending(x => x.PostedDate)
            .Take(50) // ✅ 3. Added limit to prevent Resolved items from being pushed out
            .ToList();

        // Insert system announcement (unchanged)
        announcements.Insert(0, _currentSystemAnnouncement);

        return View(announcements);
    }

    // ✅ [Added]: Admin endpoint to update announcements
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateSystemAnnouncement(string title, string content)
    {
        var userType = HttpContext.Session.GetString("UserType");

        // Simple permission check
        if (userType == "Admin")
        {
            _currentSystemAnnouncement.Title = title;
            _currentSystemAnnouncement.Content = content;
            _currentSystemAnnouncement.PostedDate = DateTime.Now;

            TempData["SystemMessageUpdated"] = "true"; // Flag to trigger frontend animation
        }

        return RedirectToAction(nameof(Index));
    }
}