using ComplaintManagementSystem.Models.ViewModels;
using ComplaintManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintManagementSystem.Controllers;

/// <summary>
/// Handles feedback operations
/// </summary>
public class FeedbackController : Controller
{
    private readonly IFeedbackService _feedbackService;
    private readonly IComplaintService _complaintService;

    public FeedbackController(IFeedbackService feedbackService, IComplaintService complaintService)
    {
        _feedbackService = feedbackService;
        _complaintService = complaintService;
    }

    // GET: /Feedback/Submit/5 (complaint ID)
    [HttpGet]
    public async Task<IActionResult> Submit(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null || userType != "Citizen")
        {
            return RedirectToAction("Login", "Account");
        }

        // Get complaint details to show context
        var complaint = await _complaintService.GetComplaintDetailsAsync(id);
        if (complaint == null)
        {
            return NotFound();
        }

        ViewBag.Complaint = complaint;

        var model = new FeedbackViewModel
        {
            ComplaintId = id
        };

        return View(model);
    }

    // POST: /Feedback/Submit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(FeedbackViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null || userType != "Citizen")
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            var complaint = await _complaintService.GetComplaintDetailsAsync(model.ComplaintId);
            ViewBag.Complaint = complaint;
            return View(model);
        }

        var success = await _feedbackService.SubmitFeedbackAsync(model, userId.Value);

        if (success)
        {
            TempData["SuccessMessage"] = "Feedback submitted successfully!";
            return RedirectToAction("Details", "Complaint", new { id = model.ComplaintId });
        }

        ModelState.AddModelError("", "Failed to submit feedback. Make sure the complaint is resolved and you haven't already submitted feedback.");
        
        var complaintData = await _complaintService.GetComplaintDetailsAsync(model.ComplaintId);
        ViewBag.Complaint = complaintData;
        return View(model);
    }

    // GET: /Feedback/View/5 (complaint ID)
    public async Task<IActionResult> View(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var feedbacks = await _feedbackService.GetFeedbackByComplaintAsync(id);
        var averageRating = await _feedbackService.GetAverageRatingAsync(id);

        ViewBag.ComplaintId = id;
        ViewBag.AverageRating = averageRating;

        return View(feedbacks);
    }
}
