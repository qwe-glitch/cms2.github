namespace ComplaintManagementSystem.Services;

/// <summary>
/// Interface for admin-related operations
/// </summary>
public interface IAdminService
{
    // DFD 9.0 - Manage Categories
    Task<bool> CreateCategoryAsync(string name, string description);
    Task<bool> UpdateCategoryAsync(int categoryId, string name, string description, bool isActive);
    Task<bool> DeleteCategoryAsync(int categoryId);
    Task<IEnumerable<object>> GetAllCategoriesAsync(string? searchQuery = null, bool onlyActive = false);

    // DFD 10.0 - Manage Staff
    Task<bool> CreateStaffAsync(string name, string email, string password, int departmentId, string phone);
    Task<bool> UpdateStaffAsync(int staffId, string name, string email, int departmentId, string phone, bool isActive);
    Task<bool> DeactivateStaffAsync(int staffId);
    Task<bool> ActivateStaffAsync(int staffId);
    Task<IEnumerable<object>> GetAllStaffAsync(string? searchQuery = null);
    Task<IEnumerable<object>> GetStaffByDepartmentAsync(int departmentId);

    // Department Management
    Task<bool> CreateDepartmentAsync(string name, string location, string description, string officePhone, string officeEmail);
    Task<bool> UpdateDepartmentAsync(int departmentId, string name, string location, string description, string officePhone, string officeEmail, bool isActive);
    Task<IEnumerable<object>> GetAllDepartmentsAsync(string? searchQuery = null, bool onlyActive = false);

    // Citizen Management
    Task<IEnumerable<object>> GetAllCitizensAsync(string? searchQuery = null);
    Task<bool> ToggleCitizenLockAsync(int citizenId);

    // Statistics
    Task<object> GetDashboardStatisticsAsync();

    // Reports
    Task<object> GetComplaintReportAsync(string period, DateTime? startDate, DateTime? endDate);
    
    // Triage Configuration
    Task<bool> UpdateCategoryRiskLevelAsync(int categoryId, string riskLevel);
    Task<bool> SetCategoryDefaultDepartmentAsync(int categoryId, int? departmentId);
    Task<bool> ToggleCitizenVulnerableFlagAsync(int citizenId);
    Task<bool> UpdateCitizenVulnerabilityInfoAsync(int citizenId, DateTime? dateOfBirth, bool hasDisability);
}
