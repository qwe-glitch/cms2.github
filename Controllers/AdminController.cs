using ComplaintManagementSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace ComplaintManagementSystem.Controllers;

/// <summary>
/// Handles admin operations (categories, staff, departments, dashboard)
/// </summary>
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly ICaseManagementService _caseManagementService;

    public AdminController(IAdminService adminService, ICaseManagementService caseManagementService)
    {
        _adminService = adminService;
        _caseManagementService = caseManagementService;
    }

    // GET: /Admin/Dashboard
    public async Task<IActionResult> Dashboard()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null || userType != "Admin")
        {
            return RedirectToAction("Login", "Account");
        }

        var statistics = await _adminService.GetDashboardStatisticsAsync();
        return View(statistics);
    }

    #region Category Management

    // GET: /Admin/Categories
    public async Task<IActionResult> Categories(string? searchQuery)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var categories = await _adminService.GetAllCategoriesAsync(searchQuery);
        ViewBag.SearchQuery = searchQuery;
        return View(categories);
    }

    // POST: /Admin/CreateCategory
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCategory(string name, string description)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _adminService.CreateCategoryAsync(name, description);
        if (success)
        {
            TempData["SuccessMessage"] = "Category created successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to create category.";
        }

        return RedirectToAction(nameof(Categories));
    }

    // POST: /Admin/UpdateCategory
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCategory(int categoryId, string name, string description, bool isActive)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _adminService.UpdateCategoryAsync(categoryId, name, description, isActive);
        if (success)
        {
            TempData["SuccessMessage"] = "Category updated successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to update category.";
        }

        return RedirectToAction(nameof(Categories));
    }

    // POST: /Admin/DeleteCategory
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int categoryId)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _adminService.DeleteCategoryAsync(categoryId);
        if (success)
        {
            TempData["SuccessMessage"] = "Category deleted successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Cannot delete category with existing complaints.";
        }

        return RedirectToAction(nameof(Categories));
    }

    #endregion

    #region Staff Management

    // GET: /Admin/Staff
    public async Task<IActionResult> Staff(string? searchQuery)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var staff = await _adminService.GetAllStaffAsync(searchQuery);
        var departments = await _adminService.GetAllDepartmentsAsync();
        
        ViewBag.Departments = departments;
        ViewBag.SearchQuery = searchQuery;
        return View(staff);
    }

    // POST: /Admin/CreateStaff
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateStaff(string name, string email, string password, int departmentId, string phone)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _adminService.CreateStaffAsync(name, email, password, departmentId, phone);
        if (success)
        {
            TempData["SuccessMessage"] = "Staff member created successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Email already exists or failed to create staff.";
        }

        return RedirectToAction(nameof(Staff));
    }

    // POST: /Admin/UpdateStaff
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStaff(int staffId, string name, string email, int departmentId, string phone, bool isActive)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _adminService.UpdateStaffAsync(staffId, name, email, departmentId, phone, isActive);
        if (success)
        {
            TempData["SuccessMessage"] = "Staff updated successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to update staff.";
        }

        return RedirectToAction(nameof(Staff));
    }

    // POST: /Admin/DeactivateStaff
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeactivateStaff(int staffId)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _adminService.DeactivateStaffAsync(staffId);
        if (success)
        {
            TempData["SuccessMessage"] = "Staff deactivated successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to deactivate staff.";
        }

        return RedirectToAction(nameof(Staff));
    }

    // POST: /Admin/ActivateStaff
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActivateStaff(int staffId)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _adminService.ActivateStaffAsync(staffId);
        if (success)
        {
            TempData["SuccessMessage"] = "Staff activated successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to activate staff.";
        }

        return RedirectToAction(nameof(Staff));
    }

    #endregion

    #region Department Management

    // GET: /Admin/Departments
    public async Task<IActionResult> Departments(string? searchQuery)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var departments = await _adminService.GetAllDepartmentsAsync(searchQuery);
        ViewBag.SearchQuery = searchQuery;
        return View(departments);
    }

    // POST: /Admin/CreateDepartment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDepartment(string name, string location, string description, string officePhone, string officeEmail)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _adminService.CreateDepartmentAsync(name, location, description, officePhone, officeEmail);
        if (success)
        {
            TempData["SuccessMessage"] = "Department created successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to create department.";
        }

        return RedirectToAction(nameof(Departments));
    }

    // POST: /Admin/UpdateDepartment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDepartment(int departmentId, string name, string location, string description, string officePhone, string officeEmail, bool isActive)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _adminService.UpdateDepartmentAsync(departmentId, name, location, description, officePhone, officeEmail, isActive);
        if (success)
        {
            TempData["SuccessMessage"] = "Department updated successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to update department.";
        }

        return RedirectToAction(nameof(Departments));
    }

    #endregion

    #region Citizen Management

    // GET: /Admin/Citizens
    public async Task<IActionResult> Citizens(string? searchQuery)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var citizens = await _adminService.GetAllCitizensAsync(searchQuery);
        ViewBag.SearchQuery = searchQuery;
        return View(citizens);
    }

    // POST: /Admin/ToggleCitizenLock
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCitizenLock(int citizenId)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _adminService.ToggleCitizenLockAsync(citizenId);
        if (success)
        {
            TempData["SuccessMessage"] = "Citizen account status updated successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to update citizen status.";
        }

        return RedirectToAction(nameof(Citizens));
    }

    #endregion



    #region Triage Configuration

    // POST: /Admin/SetCategoryRiskLevel
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCategoryRiskLevel(int categoryId, string riskLevel)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _adminService.UpdateCategoryRiskLevelAsync(categoryId, riskLevel);
        if (success)
        {
            TempData["SuccessMessage"] = "Category risk level updated successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to update category risk level.";
        }

        return RedirectToAction(nameof(Categories));
    }

    // POST: /Admin/SetCategoryDepartment
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetCategoryDepartment(int categoryId, int? departmentId)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _adminService.SetCategoryDefaultDepartmentAsync(categoryId, departmentId);
        if (success)
        {
            TempData["SuccessMessage"] = "Category department mapping updated successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to update category department.";
        }

        return RedirectToAction(nameof(Categories));
    }

    // POST: /Admin/ToggleCitizenVulnerable
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleCitizenVulnerable(int citizenId)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _adminService.ToggleCitizenVulnerableFlagAsync(citizenId);
        if (success)
        {
            TempData["SuccessMessage"] = "Citizen vulnerable status updated successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to update citizen vulnerable status.";
        }

        return RedirectToAction(nameof(Citizens));
    }

    // POST: /Admin/UpdateCitizenVulnerability
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCitizenVulnerability(int citizenId, DateTime? dateOfBirth, bool hasDisability)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _adminService.UpdateCitizenVulnerabilityInfoAsync(citizenId, dateOfBirth, hasDisability);
        if (success)
        {
            TempData["SuccessMessage"] = "Citizen vulnerability information updated successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to update citizen vulnerability information.";
        }

        return RedirectToAction(nameof(Citizens));
    }

    #endregion

    #region Case Management

    // POST: /Admin/LinkComplaints
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LinkComplaints(int sourceComplaintId, int targetComplaintId, string linkType, string? notes)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var success = await _caseManagementService.LinkComplaintsAsync(
            sourceComplaintId, targetComplaintId, linkType, notes, userId, "Admin");

        if (success)
        {
            TempData["SuccessMessage"] = $"Complaints linked successfully as {linkType}!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to link complaints. They may already be linked or one doesn't exist.";
        }

        return RedirectToAction("Details", "Complaint", new { id = sourceComplaintId });
    }

    // POST: /Admin/UnlinkComplaints
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnlinkComplaints(int linkId, int complaintId)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var success = await _caseManagementService.UnlinkComplaintsAsync(linkId);

        if (success)
        {
            TempData["SuccessMessage"] = "Complaints unlinked successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to unlink complaints.";
        }

        return RedirectToAction("LinkedComplaints", "Complaint", new { id = complaintId });
    }

    // POST: /Admin/MarkAsDuplicate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsDuplicate(int originalComplaintId, int duplicateComplaintId)
    {
        if (!IsAdmin())
        {
            return RedirectToAction("Login", "Account");
        }

        var userId = HttpContext.Session.GetInt32("UserId")!.Value;
        var success = await _caseManagementService.MarkAsDuplicateAsync(
            originalComplaintId, duplicateComplaintId, userId, "Admin");

        if (success)
        {
            TempData["SuccessMessage"] = "Complaint marked as duplicate and closed successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to mark complaint as duplicate.";
        }

        return RedirectToAction("Details", "Complaint", new { id = originalComplaintId });
    }

    #endregion

    #region Helper Methods

    private bool IsAdmin()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");
        return userId != null && userType == "Admin";
    }

    #endregion
}
