using ComplaintManagementSystem.Models.ViewModels;
using ComplaintManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintManagementSystem.Controllers;

/// <summary>
/// Handles complaint operations for citizens and staff
/// </summary>
public class ComplaintController : Controller
{
    private readonly IComplaintService _complaintService;
    private readonly IAdminService _adminService;
    private readonly ICaseManagementService _caseManagementService;
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ISpamDetectionService _spamDetectionService;

    public ComplaintController(IComplaintService complaintService, IAdminService adminService, 
        ICaseManagementService caseManagementService, IKnowledgeBaseService knowledgeBaseService,
        ISpamDetectionService spamDetectionService)
    {
        _complaintService = complaintService;
        _adminService = adminService;
        _caseManagementService = caseManagementService;
        _knowledgeBaseService = knowledgeBaseService;
        _spamDetectionService = spamDetectionService;
    }

    // GET: /Complaint/Index
    public async Task<IActionResult> Index()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null || userType == null)
        {
            return RedirectToAction("Login", "Account");
        }

        IEnumerable<object> complaints;

        switch (userType)
        {
            case "Citizen":
                complaints = await _complaintService.GetComplaintsByCitizenAsync(userId.Value);
                break;
            case "Staff":
                complaints = await _complaintService.GetComplaintsByStaffAsync(userId.Value);
                break;
            case "Admin":
                complaints = await _complaintService.GetAllComplaintsAsync();
                break;
            default:
                return RedirectToAction("Login", "Account");
        }

        ViewBag.UserType = userType;
        return View(complaints);
    }

    // GET: /Complaint/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Use user context for privacy handling - masks citizen info when viewing other citizens' complaints
        var complaint = await _complaintService.GetComplaintDetailsAsync(id, userId, userType);
        if (complaint == null)
        {
            return NotFound();
        }

        ViewBag.UserType = userType;
        return View(complaint);
    }

    // GET: /Complaint/PrintDetails/5
    public async Task<IActionResult> PrintDetails(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");
        
        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        // Use user context for privacy handling
        var complaint = await _complaintService.GetComplaintDetailsAsync(id, userId, userType);
        if (complaint == null)
        {
            return NotFound();
        }

        return View(complaint);
    }

    // GET: /Complaint/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null || userType != "Citizen")
        {
            return RedirectToAction("Login", "Account");
        }

        // Load categories for dropdown
        var categories = await _adminService.GetAllCategoriesAsync(null, true);
        ViewBag.Categories = categories;

        return View();
    }

    // POST: /Complaint/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ComplaintSubmissionViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null || userType != "Citizen")
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            var categories = await _adminService.GetAllCategoriesAsync(null, true);
            ViewBag.Categories = categories;
            return View(model);
        }

        // Check for sensitive content
        if (_spamDetectionService.CheckSensitiveContent(model.Title, out var sensitiveTitle))
        {
            ModelState.AddModelError("Title", $"Title contains sensitive content: '{sensitiveTitle}'");
        }
        if (_spamDetectionService.CheckSensitiveContent(model.Description, out var sensitiveDesc))
        {
            ModelState.AddModelError("Description", $"Description contains sensitive content: '{sensitiveDesc}'");
        }

        if (!ModelState.IsValid)
        {
            var categories = await _adminService.GetAllCategoriesAsync(null, true);
            ViewBag.Categories = categories;
            return View(model);
        }

        // Check for spam before submitting
        var spamCheck = await _spamDetectionService.CheckForSpamAsync(
            model.Title, model.Description, userId.Value);
        
        if (spamCheck.IsSpam)
        {
            ModelState.AddModelError("", spamCheck.Reason);
            ViewBag.SpamWarning = true;
            ViewBag.SpamFlags = spamCheck.Flags;
            var categories = await _adminService.GetAllCategoriesAsync(null, true);
            ViewBag.Categories = categories;
            return View(model);
        }

        // Record successful submission for rate limiting
        _spamDetectionService.RecordSubmissionAttempt(userId.Value);

        var complaintId = await _complaintService.SubmitComplaintAsync(model, userId.Value);

        TempData["SuccessMessage"] = "Complaint submitted successfully!";
        return RedirectToAction(nameof(Details), new { id = complaintId });
    }

    // GET: /Complaint/Update/5
    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null || (userType != "Staff" && userType != "Admin"))
        {
            return RedirectToAction("Login", "Account");
        }

        var complaint = await _complaintService.GetComplaintForUpdateAsync(id);
        if (complaint == null)
        {
            return NotFound();
        }

        // Load staff for assignment (if admin)
        if (userType == "Admin")
        {
            var staff = await _adminService.GetAllStaffAsync();
            ViewBag.Staff = staff;
        }

        return View(complaint);
    }

    // POST: /Complaint/Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ComplaintUpdateViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null || (userType != "Staff" && userType != "Admin"))
        {
            return RedirectToAction("Login", "Account");
        }

        if (!ModelState.IsValid)
        {
            if (userType == "Admin")
            {
                var staff = await _adminService.GetAllStaffAsync();
                ViewBag.Staff = staff;
            }
            return View(model);
        }

        // Check if complaint is already finalized
        var currentComplaint = await _complaintService.GetComplaintForUpdateAsync(model.ComplaintId);
        if (currentComplaint != null)
        {
            // 1. Check if already finalized
            if (currentComplaint.Status == "Resolved" || currentComplaint.Status == "Rejected" || currentComplaint.Status == "Closed - Duplicate")
            {
                ModelState.AddModelError("", $"Complaint is already finalized as {currentComplaint.Status} and cannot be updated.");
                if (userType == "Admin")
                {
                    var staff = await _adminService.GetAllStaffAsync();
                    ViewBag.Staff = staff;
                }
                return View(model);
            }

            // 2. Validate Status Transitions
            bool isValidTransition = false;
            if (currentComplaint.Status == "Pending")
            {
                // Pending -> Pending (no change), In Progress, or Rejected
                if (model.Status == "Pending" || model.Status == "In Progress" || model.Status == "Rejected")
                {
                    isValidTransition = true;
                }
            }
            else if (currentComplaint.Status == "In Progress")
            {
                // In Progress -> In Progress (no change) or Resolved
                if (model.Status == "In Progress" || model.Status == "Resolved")
                {
                    isValidTransition = true;
                }
            }

            if (!isValidTransition)
            {
                ModelState.AddModelError("Status", $"Invalid status transition from '{currentComplaint.Status}' to '{model.Status}'.");
                if (userType == "Admin")
                {
                    var staff = await _adminService.GetAllStaffAsync();
                    ViewBag.Staff = staff;
                }
                return View(model);
            }
            
            // 3. Explicitly block "Closed - Duplicate" from manual selection (though UI should hide it too)
            if (model.Status == "Closed - Duplicate")
            {
                ModelState.AddModelError("Status", "The 'Closed - Duplicate' status cannot be selected manually. Please use the Duplicate management tools.");
                if (userType == "Admin")
                {
                    var staff = await _adminService.GetAllStaffAsync();
                    ViewBag.Staff = staff;
                }
                return View(model);
            }
        }

        var success = await _complaintService.UpdateComplaintAsync(model, userId.Value);

        if (success)
        {
            // Upload attachments if provided
            if (model.Attachments != null && model.Attachments.Count > 0)
            {
                await _complaintService.UploadAttachmentsAsync(model.ComplaintId, model.Attachments);
            }

            TempData["SuccessMessage"] = "Complaint updated successfully!";
            return RedirectToAction(nameof(Details), new { id = model.ComplaintId });
        }

        ModelState.AddModelError("", "Failed to update complaint");
        return View(model);
    }

    // GET: /Complaint/PotentialDuplicates/5
    public async Task<IActionResult> PotentialDuplicates(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var duplicates = await _caseManagementService.FindPotentialDuplicatesAsync(id);
            ViewBag.UserType = userType;
            return View(duplicates);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    // GET: /Complaint/LinkedComplaints/5
    public async Task<IActionResult> LinkedComplaints(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null)
        {
            return RedirectToAction("Login", "Account");
        }

        var linkedComplaints = await _caseManagementService.GetLinkedComplaintsAsync(id);
        
        ViewBag.ComplaintId = id;
        ViewBag.UserType = userType;
        return View(linkedComplaints);
    }

    // POST: /Complaint/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        // Only admins can delete complaints
        if (userId == null || userType != "Admin")
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _complaintService.DeleteComplaintAsync(id);

        if (success)
        {
            TempData["SuccessMessage"] = "Complaint deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        TempData["ErrorMessage"] = "Failed to delete complaint. Please try again.";
        return RedirectToAction(nameof(Details), new { id });
    }
    // GET: /Complaint/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null || userType != "Citizen")
        {
            return RedirectToAction("Login", "Account");
        }

        var complaint = await _complaintService.GetComplaintForEditAsync(id, userId.Value);
        if (complaint == null)
        {
            return NotFound();
        }

        // Load categories for dropdown
        var categories = await _adminService.GetAllCategoriesAsync(null, true);
        ViewBag.Categories = categories;

        return View(complaint);
    }

    // POST: /Complaint/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ComplaintEditViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null || userType != "Citizen")
        {
            return RedirectToAction("Login", "Account");
        }

        ComplaintEditViewModel? currentCompl = null;

        if (!ModelState.IsValid)
        {
            var categories = await _adminService.GetAllCategoriesAsync(null, true);
            ViewBag.Categories = categories;
            // Repopulate existing attachments
            currentCompl = await _complaintService.GetComplaintForEditAsync(model.ComplaintId, userId.Value);
            if (currentCompl != null) model.ExistingAttachments = currentCompl.ExistingAttachments;
            return View(model);
        }

        // Check for sensitive content
        if (_spamDetectionService.CheckSensitiveContent(model.Title, out var sensitiveEditTitle))
        {
            ModelState.AddModelError("Title", $"Title contains sensitive content: '{sensitiveEditTitle}'");
        }
        if (_spamDetectionService.CheckSensitiveContent(model.Description, out var sensitiveEditDesc))
        {
            ModelState.AddModelError("Description", $"Description contains sensitive content: '{sensitiveEditDesc}'");
        }

        if (!ModelState.IsValid)
        {
            var categories = await _adminService.GetAllCategoriesAsync(null, true);
            ViewBag.Categories = categories;
            // Repopulate existing attachments
            currentCompl = await _complaintService.GetComplaintForEditAsync(model.ComplaintId, userId.Value);
            if (currentCompl != null) model.ExistingAttachments = currentCompl.ExistingAttachments;
            return View(model);
        }

        // Check for spam (repeated characters, keyboard patterns, gibberish)
        var spamCheck = await _spamDetectionService.CheckForSpamAsync(
            model.Title, model.Description, userId.Value);
        
        if (spamCheck.IsSpam)
        {
            ModelState.AddModelError("", spamCheck.Reason);
            ViewBag.SpamWarning = true;
            ViewBag.SpamFlags = spamCheck.Flags;
            var categories = await _adminService.GetAllCategoriesAsync(null, true);
            ViewBag.Categories = categories;
            return View(model);
        }

        var success = await _complaintService.UpdateComplaintDetailsAsync(model, userId.Value);

        if (success)
        {
            TempData["SuccessMessage"] = "Complaint updated successfully!";
            return RedirectToAction(nameof(Details), new { id = model.ComplaintId });
        }

        ModelState.AddModelError("", "Failed to update complaint or complaint is no longer pending.");
        var cats = await _adminService.GetAllCategoriesAsync(null, true);
        ViewBag.Categories = cats;
        // Repopulate existing attachments for the view
        currentCompl = await _complaintService.GetComplaintForEditAsync(model.ComplaintId, userId.Value);
        if (currentCompl != null)
        {
            model.ExistingAttachments = currentCompl.ExistingAttachments;
        }
        return View(model);
    }

    // GET: /Complaint/SearchKnowledgeBase
    [HttpGet]
    public async Task<IActionResult> SearchKnowledgeBase(string query, int? categoryId)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
        {
            return Json(new { suggestions = new List<object>() });
        }

        var suggestions = await _knowledgeBaseService.SearchSuggestionsAsync(query, categoryId, 5);
        
        return Json(new { 
            suggestions = suggestions.Select(s => new {
                complaintId = s.ComplaintId,
                title = s.Title,
                description = s.Description,
                resolutionSummary = s.ResolutionSummary,
                categoryName = s.CategoryName,
                resolvedAt = s.ResolvedAt.ToString("MMM dd, yyyy"),
                relevanceScore = s.RelevanceScore
            })
        });
    }
}

