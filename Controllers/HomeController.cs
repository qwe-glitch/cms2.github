using Microsoft.AspNetCore.Mvc;
using ComplaintManagementSystem.Models;
using ComplaintManagementSystem.Services;
using Microsoft.EntityFrameworkCore;

namespace Complaint_Management_System.Controllers;

public class HomeController : Controller
{
    private readonly DB _db;
    private readonly IComplaintService _complaintService;

    public HomeController(DB db, IComplaintService complaintService)
    {
        _db = db;
        _complaintService = complaintService;
    }

    public async Task<IActionResult> Index()
    {
        // Fetch categories for filter
        ViewBag.Categories = await _db.Categories.OrderBy(c => c.CategoryName).ToListAsync();

        // Fetch all public complaints (without citizen personal info)
        var complaints = await _complaintService.GetPublicComplaintsAsync();
        return View(complaints);
    }
}
