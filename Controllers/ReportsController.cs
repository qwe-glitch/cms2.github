using ComplaintManagementSystem.Models;
using ComplaintManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagementSystem.Controllers;

/// <summary>
/// Handles reporting and analytics operations
/// </summary>
public class ReportsController : Controller
{
    private readonly IReportingService _reportingService;
    private readonly DB _context;

    public ReportsController(IReportingService reportingService, DB context)
    {
        _reportingService = reportingService;
        _context = context;
    }

    // GET: /Reports/Analytics
    public async Task<IActionResult> Analytics(DateTime? startDate, DateTime? endDate)
    {
        if (!IsAdminOrStaff())
        {
            return RedirectToAction("Login", "Account");
        }

        var data = await _reportingService.GetAnalyticsDashboardDataAsync(startDate, endDate);
        
        ViewBag.StartDate = startDate ?? DateTime.UtcNow.AddDays(-30);
        ViewBag.EndDate = endDate ?? DateTime.UtcNow;
        
        return View(data);
    }

    // GET: /Reports/SlaCompliance
    public async Task<IActionResult> SlaCompliance(DateTime? startDate, DateTime? endDate, int? categoryId, int? departmentId)
    {
        if (!IsAdminOrStaff())
        {
            return RedirectToAction("Login", "Account");
        }

        var data = await _reportingService.GetSlaComplianceReportAsync(startDate, endDate, categoryId, departmentId);
        
        ViewBag.StartDate = startDate ?? DateTime.UtcNow.AddDays(-30);
        ViewBag.EndDate = endDate ?? DateTime.UtcNow;
        
        return View(data);
    }

    // GET: /Reports/TeamWorkload
    public async Task<IActionResult> TeamWorkload(DateTime? startDate, DateTime? endDate, int? departmentId)
    {
        if (!IsAdminOrStaff())
        {
            return RedirectToAction("Login", "Account");
        }

        var data = await _reportingService.GetTeamWorkloadMetricsAsync(startDate, endDate, departmentId);
        
        ViewBag.StartDate = startDate ?? DateTime.UtcNow.AddDays(-30);
        ViewBag.EndDate = endDate ?? DateTime.UtcNow;
        
        return View(data);
    }

    // GET: /Reports/GeographicData (JSON endpoint for map)
    public async Task<IActionResult> GeographicData(DateTime? startDate, DateTime? endDate, int? categoryId)
    {
        if (!IsAdminOrStaff())
        {
            return Json(new { error = "Unauthorized" });
        }

        var data = await _reportingService.GetGeographicHotspotsAsync(startDate, endDate, categoryId);
        return Json(data);
    }

    // GET: /Reports/ExportExcel
    public async Task<IActionResult> ExportExcel(DateTime? startDate, DateTime? endDate, string? status, int? categoryId, int? departmentId)
    {
        if (!IsAdminOrStaff())
        {
            return RedirectToAction("Login", "Account");
        }

        var excelData = await _reportingService.ExportComplaintsToExcelAsync(startDate, endDate, status, categoryId, departmentId);
        
        var fileName = $"complaints_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
        return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // GET: /Reports/ExportPDF
    public async Task<IActionResult> ExportPDF(string reportType = "sla", DateTime? startDate = null, DateTime? endDate = null)
    {
        if (!IsAdminOrStaff())
        {
            return RedirectToAction("Login", "Account");
        }

        var pdfData = await _reportingService.ExportReportToPDFAsync(reportType, startDate, endDate);
        
        var fileName = $"{reportType}_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
        return File(pdfData, "application/pdf", fileName);
    }

    // GET: /Reports/Explore
    public async Task<IActionResult> Explore(DateTime? startDate, DateTime? endDate, string? status, int? categoryId, int? departmentId)
    {
        if (!IsAdminOrStaff())
        {
            return RedirectToAction("Login", "Account");
        }

        var data = await _reportingService.GetComplaintReportAsync(startDate, endDate, status, categoryId, departmentId);
        
        ViewBag.StartDate = startDate ?? DateTime.UtcNow.AddDays(-30);
        ViewBag.EndDate = endDate ?? DateTime.UtcNow;
        ViewBag.CurrentStatus = status;
        ViewBag.CurrentCategoryId = categoryId;
        ViewBag.CurrentDepartmentId = departmentId;

        // Populate dropdowns
        ViewBag.Categories = await _context.Categories.ToListAsync();
        ViewBag.Departments = await _context.Departments.ToListAsync();
        
        return View(data);
    }

    // Helper method to check if user is admin or staff
    private bool IsAdminOrStaff()
    {
        var userRole = HttpContext.Session.GetString("UserType");
        return userRole == "Admin" || userRole == "Staff";
    }
}
