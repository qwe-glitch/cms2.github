namespace ComplaintManagementSystem.Services;

/// <summary>
/// Interface for intelligent complaint triage and routing operations
/// </summary>
public interface ITriageService
{
    /// <summary>
    /// Main triage orchestrator - assesses severity, determines priority, routes to department
    /// </summary>
    Task<TriageResult> AssessComplaintAsync(int complaintId);

    /// <summary>
    /// Calculate severity score (0-100) based on keywords, patterns, and category risk
    /// </summary>
    Task<int> CalculateSeverityScoreAsync(string title, string description, int categoryId, int citizenId);

    /// <summary>
    /// Determine automatic priority based on severity score and vulnerability
    /// </summary>
    Task<string> DetermineAutoPriorityAsync(int severityScore, bool isVulnerable);

    /// <summary>
    /// Route complaint to the most suitable department with load balancing
    /// </summary>
    Task<int?> RouteToMostSuitableDepartmentAsync(int categoryId, int? currentDepartmentId);

    /// <summary>
    /// Check if citizen is vulnerable (elderly, disabled, or flagged)
    /// </summary>
    Task<bool> CheckVulnerableReporterAsync(int citizenId);

    /// <summary>
    /// Generate triage notes explaining the assessment
    /// </summary>
    Task<string> GenerateTriageNotesAsync(int severityScore, bool isVulnerable, string priority, int? departmentId);
}

/// <summary>
/// Result of triage assessment
/// </summary>
public class TriageResult
{
    public int SeverityScore { get; set; }
    public string Priority { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
    public string TriageNotes { get; set; } = string.Empty;
    public bool IsVulnerable { get; set; }
}
