namespace ComplaintManagementSystem.Services;

/// <summary>
/// Interface for reporting and analytics operations
/// </summary>
public interface IReportingService
{
    // SLA Compliance Reporting
    Task<object> GetSlaComplianceReportAsync(DateTime? startDate = null, DateTime? endDate = null, int? categoryId = null, int? departmentId = null);
    
    // Team Workload Metrics
    Task<object> GetTeamWorkloadMetricsAsync(DateTime? startDate = null, DateTime? endDate = null, int? departmentId = null);
    
    // Geographic Hotspots
    Task<object> GetGeographicHotspotsAsync(DateTime? startDate = null, DateTime? endDate = null, int? categoryId = null);
    
    // Export Functions
    Task<byte[]> ExportComplaintsToExcelAsync(DateTime? startDate = null, DateTime? endDate = null, string? status = null, int? categoryId = null, int? departmentId = null);
    Task<byte[]> ExportReportToPDFAsync(string reportType, DateTime? startDate = null, DateTime? endDate = null);
    
    // Analytics Dashboard
    Task<object> GetAnalyticsDashboardDataAsync(DateTime? startDate = null, DateTime? endDate = null);

    // Complaint Report
    Task<object> GetComplaintReportAsync(DateTime? startDate = null, DateTime? endDate = null, string? status = null, int? categoryId = null, int? departmentId = null);
}
