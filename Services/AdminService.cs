using ComplaintManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagementSystem.Services;

/// <summary>
/// Implementation of admin-related operations
/// </summary>
public class AdminService : IAdminService
{
    private readonly DB _context;

    public AdminService(DB context)
    {
        _context = context;
    }

    #region Category Management - DFD 9.0

    public async Task<bool> CreateCategoryAsync(string name, string description)
    {
        var category = new Category
        {
            CategoryName = name,
            Description = description
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateCategoryAsync(int categoryId, string name, string description, bool isActive)
    {
        var category = await _context.Categories.FindAsync(categoryId);
        if (category == null) return false;

        category.CategoryName = name;
        category.Description = description;
        category.IsActive = isActive;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteCategoryAsync(int categoryId)
    {
        var category = await _context.Categories.FindAsync(categoryId);
        if (category == null) return false;

        // Check if category has complaints
        var hasComplaints = await _context.Complaints.AnyAsync(c => c.CategoryId == categoryId);
        if (hasComplaints) return false; // Cannot delete category with complaints

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<object>> GetAllCategoriesAsync(string? searchQuery = null, bool onlyActive = false)
    {
        var query = _context.Categories.AsQueryable();

        if (onlyActive)
        {
            query = query.Where(c => c.IsActive);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(c => c.CategoryName.Contains(searchQuery) || (c.Description != null && c.Description.Contains(searchQuery)));
        }

        return await query
            .Include(c => c.DefaultDepartment)
            .Select(c => new
            {
                c.CategoryId,
                c.CategoryName,
                c.Description,
                c.RiskLevel,
                c.IsActive,
                c.DefaultDepartmentId,
                DefaultDepartmentName = c.DefaultDepartment != null ? c.DefaultDepartment.DepartmentName : null,
                ComplaintCount = c.Complaints.Count()
            })
            .ToListAsync();
    }

    #endregion

    #region Staff Management - DFD 10.0

    public async Task<bool> CreateStaffAsync(string name, string email, string password, int departmentId, string phone)
    {
        // Check if email already exists
        var exists = await _context.Staff.AnyAsync(s => s.Email == email);
        if (exists) return false;

        var staff = new Staff
        {
            Name = name,
            Email = email,
            PasswordHash = SecurityHelper.HashPassword(password),
            DepartmentId = departmentId,
            Phone = phone,
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        _context.Staff.Add(staff);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStaffAsync(int staffId, string name, string email, int departmentId, string phone, bool isActive)
    {
        var staff = await _context.Staff.FindAsync(staffId);
        if (staff == null) return false;

        staff.Name = name;
        staff.Email = email;
        staff.DepartmentId = departmentId;
        staff.Phone = phone;
        staff.IsActive = isActive;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeactivateStaffAsync(int staffId)
    {
        var staff = await _context.Staff.FindAsync(staffId);
        if (staff == null) return false;

        staff.IsActive = false;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ActivateStaffAsync(int staffId)
    {
        var staff = await _context.Staff.FindAsync(staffId);
        if (staff == null) return false;

        staff.IsActive = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<object>> GetAllStaffAsync(string? searchQuery = null)
    {
        var query = _context.Staff.Include(s => s.Department).AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(s => s.Name.Contains(searchQuery) || s.Email.Contains(searchQuery) || s.Department!.DepartmentName.Contains(searchQuery));
        }

        return await query
            .Select(s => new
            {
                s.StaffId,
                s.Name,
                s.Email,
                s.Phone,
                s.DepartmentId,
                DepartmentName = s.Department!.DepartmentName,
                s.IsActive,
                s.CreatedAt,
                AssignedComplaints = s.Complaints.Count()
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<object>> GetStaffByDepartmentAsync(int departmentId)
    {
        return await _context.Staff
            .Where(s => s.DepartmentId == departmentId && s.IsActive)
            .Select(s => new
            {
                s.StaffId,
                s.Name,
                s.Email,
                AssignedComplaints = s.Complaints.Count()
            })
            .ToListAsync();
    }


    #endregion

    #region Citizen Management
    
    public async Task<IEnumerable<object>> GetAllCitizensAsync(string? searchQuery = null)
    {
        var query = _context.Citizens.AsQueryable();

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(c => c.Name.Contains(searchQuery) || c.Email.Contains(searchQuery) || (c.Phone != null && c.Phone.Contains(searchQuery)) || (c.Address != null && c.Address.Contains(searchQuery)));
        }

        return await query
            .Select(c => new
            {
                c.CitizenId,
                c.Name,
                c.Email,
                c.Phone,
                c.Address,
                c.CreatedAt,
                c.IsLocked,
                c.FailedLoginAttempts,
                c.IsVulnerable,
                c.DateOfBirth,
                c.HasDisability,
                ComplaintCount = c.Complaints.Count()
            })
            .ToListAsync();
    }

    public async Task<bool> ToggleCitizenLockAsync(int citizenId)
    {
        var citizen = await _context.Citizens.FindAsync(citizenId);
        if (citizen == null) return false;

        citizen.IsLocked = !citizen.IsLocked;
        // If unlocking, reset failed attempts
        if (!citizen.IsLocked)
        {
            citizen.FailedLoginAttempts = 0;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

    #region Department Management

    public async Task<bool> CreateDepartmentAsync(string name, string location, string description, string officePhone, string officeEmail)
    {
        var department = new Department
        {
            DepartmentName = name,
            Location = location,
            Description = description,
            OfficePhone = officePhone,
            OfficeEmail = officeEmail,
            CreatedAt = DateTime.Now
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateDepartmentAsync(int departmentId, string name, string location, string description, string officePhone, string officeEmail, bool isActive)
    {
        var department = await _context.Departments.FindAsync(departmentId);
        if (department == null) return false;

        department.DepartmentName = name;
        department.Location = location;
        department.Description = description;
        department.OfficePhone = officePhone;
        department.OfficeEmail = officeEmail;
        department.IsActive = isActive;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<object>> GetAllDepartmentsAsync(string? searchQuery = null, bool onlyActive = false)
    {
        var query = _context.Departments.AsQueryable();

        if (onlyActive)
        {
            query = query.Where(d => d.IsActive);
        }

        if (!string.IsNullOrEmpty(searchQuery))
        {
            query = query.Where(d => d.DepartmentName.Contains(searchQuery) || (d.Location != null && d.Location.Contains(searchQuery)) || (d.Description != null && d.Description.Contains(searchQuery)));
        }

        return await query
            .Select(d => new
            {
                d.DepartmentId,
                d.DepartmentName,
                d.Location,
                d.Description,
                d.OfficePhone,
                d.OfficeEmail,
                d.IsActive,
                StaffCount = d.Staff.Count(s => s.IsActive)
            })
            .ToListAsync();
    }

    #endregion

    #region Statistics

    public async Task<object> GetDashboardStatisticsAsync()
    {
        var totalComplaints = await _context.Complaints.CountAsync();
        var pendingComplaints = await _context.Complaints.CountAsync(c => c.Status == "Pending");
        var inProgressComplaints = await _context.Complaints.CountAsync(c => c.Status == "In Progress");
        var resolvedComplaints = await _context.Complaints.CountAsync(c => c.Status == "Resolved");
        var totalCitizens = await _context.Citizens.CountAsync();
        var totalStaff = await _context.Staff.CountAsync(s => s.IsActive);
        var totalCategories = await _context.Categories.CountAsync();

        // Complaints by category
        var complaintsByCategory = await _context.Complaints
            .GroupBy(c => c.Category!.CategoryName)
            .Select(g => new
            {
                Category = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        // Recent complaints
        var recentComplaints = await _context.Complaints
            .OrderByDescending(c => c.SubmittedAt)
            .Take(10)
            .Include(c => c.Citizen)
            .Include(c => c.Category)
            .Select(c => new
            {
                c.ComplaintId,
                c.Title,
                c.Status,
                CategoryName = c.Category!.CategoryName,
                CitizenName = c.Citizen!.Name,
                c.SubmittedAt
            })
            .ToListAsync();

        // Weekly Activity - Last 7 days (Mon-Sun)
        var today = DateTime.Today;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
        if (today.DayOfWeek == DayOfWeek.Sunday)
        {
            startOfWeek = startOfWeek.AddDays(-7);
        }

        var weeklyResolved = new List<int>();
        var weeklyInProgress = new List<int>();
        var weeklyPending = new List<int>();

        for (int i = 0; i < 7; i++)
        {
            var currentDay = startOfWeek.AddDays(i);
            var nextDay = currentDay.AddDays(1);

            // Count complaints by current status that were last updated on this day
            var resolved = await _context.Complaints
                .CountAsync(c => c.Status == "Resolved" && 
                               c.ResolvedAt.HasValue && 
                               c.ResolvedAt.Value >= currentDay && 
                               c.ResolvedAt.Value < nextDay);

            var inProgress = await _context.Complaints
                .CountAsync(c => c.Status == "In Progress" && 
                               c.UpdatedAt.HasValue && 
                               c.UpdatedAt.Value >= currentDay && 
                               c.UpdatedAt.Value < nextDay);

            var pending = await _context.Complaints
                .CountAsync(c => c.Status == "Pending" && 
                               c.SubmittedAt >= currentDay && 
                               c.SubmittedAt < nextDay);

            weeklyResolved.Add(resolved);
            weeklyInProgress.Add(inProgress);
            weeklyPending.Add(pending);
        }

        return new
        {
            TotalComplaints = totalComplaints,
            PendingComplaints = pendingComplaints,
            InProgressComplaints = inProgressComplaints,
            ResolvedComplaints = resolvedComplaints,
            TotalCitizens = totalCitizens,
            TotalStaff = totalStaff,
            TotalCategories = totalCategories,
            ComplaintsByCategory = complaintsByCategory,
            RecentComplaints = recentComplaints,
            WeeklyResolved = weeklyResolved,
            WeeklyInProgress = weeklyInProgress,
            WeeklyPending = weeklyPending
        };
    }

    public async Task<object> GetComplaintReportAsync(string period, DateTime? startDate, DateTime? endDate)
    {
        // Default to current date if not provided
        var referenceDate = startDate ?? DateTime.Now;
        DateTime filterStartDate;
        DateTime filterEndDate;

        // Determine date range based on period
        switch (period?.ToLower())
        {
            case "daily":
                filterStartDate = referenceDate.Date;
                filterEndDate = referenceDate.Date.AddDays(1).AddSeconds(-1);
                break;

            case "weekly":
                filterStartDate = referenceDate.Date.AddDays(-6); // Last 7 days including today
                filterEndDate = referenceDate.Date.AddDays(1).AddSeconds(-1);
                break;

            case "monthly":
                filterStartDate = new DateTime(referenceDate.Year, referenceDate.Month, 1);
                filterEndDate = filterStartDate.AddMonths(1).AddSeconds(-1);
                break;

            case "yearly":
                filterStartDate = new DateTime(referenceDate.Year, 1, 1);
                filterEndDate = new DateTime(referenceDate.Year, 12, 31, 23, 59, 59);
                break;

            case "custom":
                if (startDate.HasValue && endDate.HasValue)
                {
                    filterStartDate = startDate.Value.Date;
                    filterEndDate = endDate.Value.Date.AddDays(1).AddSeconds(-1);
                }
                else
                {
                    // Default to monthly if custom dates not properly provided
                    filterStartDate = new DateTime(referenceDate.Year, referenceDate.Month, 1);
                    filterEndDate = filterStartDate.AddMonths(1).AddSeconds(-1);
                }
                break;

            default:
                // Default to monthly
                filterStartDate = new DateTime(referenceDate.Year, referenceDate.Month, 1);
                filterEndDate = filterStartDate.AddMonths(1).AddSeconds(-1);
                period = "monthly";
                break;
        }

        // Get complaints within the date range
        var complaintsInPeriod = await _context.Complaints
            .Include(c => c.Category)
            .Where(c => c.SubmittedAt >= filterStartDate && c.SubmittedAt <= filterEndDate)
            .ToListAsync();

        // Total complaints in period
        var totalComplaints = complaintsInPeriod.Count;

        // Status breakdown
        var pendingComplaints = complaintsInPeriod.Count(c => c.Status == "Pending");
        var inProgressComplaints = complaintsInPeriod.Count(c => c.Status == "In Progress");
        var resolvedComplaints = complaintsInPeriod.Count(c => c.Status == "Resolved");

        // Complaints by category with counts and percentages
        var complaintsByCategory = complaintsInPeriod
            .GroupBy(c => new { c.CategoryId, CategoryName = c.Category!.CategoryName })
            .Select(g => new
            {
                CategoryId = g.Key.CategoryId,
                Category = g.Key.CategoryName,
                Count = g.Count(),
                Percentage = totalComplaints > 0 ? Math.Round((double)g.Count() / totalComplaints * 100, 1) : 0
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        return new
        {
            Period = period,
            StartDate = filterStartDate,
            EndDate = filterEndDate,
            TotalComplaints = totalComplaints,
            PendingComplaints = pendingComplaints,
            InProgressComplaints = inProgressComplaints,
            ResolvedComplaints = resolvedComplaints,
            ComplaintsByCategory = complaintsByCategory
        };
    }

    #endregion

    #region Triage Configuration

    public async Task<bool> UpdateCategoryRiskLevelAsync(int categoryId, string riskLevel)
    {
        var category = await _context.Categories.FindAsync(categoryId);
        if (category == null) return false;

        // Validate risk level
        if (riskLevel != "Low" && riskLevel != "Medium" && riskLevel != "High")
        {
            return false;
        }

        category.RiskLevel = riskLevel;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetCategoryDefaultDepartmentAsync(int categoryId, int? departmentId)
    {
        var category = await _context.Categories.FindAsync(categoryId);
        if (category == null) return false;

        // Validate department exists if provided
        if (departmentId.HasValue)
        {
            var departmentExists = await _context.Departments.AnyAsync(d => d.DepartmentId == departmentId.Value);
            if (!departmentExists) return false;
        }

        category.DefaultDepartmentId = departmentId;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleCitizenVulnerableFlagAsync(int citizenId)
    {
        var citizen = await _context.Citizens.FindAsync(citizenId);
        if (citizen == null) return false;

        citizen.IsVulnerable = !citizen.IsVulnerable;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateCitizenVulnerabilityInfoAsync(int citizenId, DateTime? dateOfBirth, bool hasDisability)
    {
        var citizen = await _context.Citizens.FindAsync(citizenId);
        if (citizen == null) return false;

        citizen.DateOfBirth = dateOfBirth;
        citizen.HasDisability = hasDisability;
        await _context.SaveChangesAsync();
        return true;
    }

    #endregion

}
