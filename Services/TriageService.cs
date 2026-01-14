using ComplaintManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ComplaintManagementSystem.Services;

/// <summary>
/// Implementation of intelligent complaint triage and routing
/// </summary>
public class TriageService : ITriageService
{
    private readonly DB _context;
    
    // Keywords for severity assessment
    private readonly string[] _urgentKeywords = {
        "emergency", "urgent", "danger", "critical", "immediate", "asap",
        "life-threatening", "injury", "injured", "accident", "severe",
        "flooding", "fire", "gas leak", "explosion", "electrical hazard"
    };

    private readonly string[] _highKeywords = {
        "broken", "leak", "unsafe", "hazard", "risk", "damaged",
        "blocked", "overflowing", "major", "serious", "urgent"
    };

    private readonly string[] _moderateKeywords = {
        "repair", "fix", "problem", "issue", "concern", "needs attention",
        "faulty", "not working", "malfunctioning"
    };

    public TriageService(DB context)
    {
        _context = context;
    }

    public async Task<TriageResult> AssessComplaintAsync(int complaintId)
    {
        var complaint = await _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.Citizen)
            .FirstOrDefaultAsync(c => c.ComplaintId == complaintId);

        if (complaint == null)
        {
            throw new ArgumentException($"Complaint with ID {complaintId} not found");
        }

        // 1. Calculate severity score
        var severityScore = await CalculateSeverityScoreAsync(
            complaint.Title,
            complaint.Description,
            complaint.CategoryId,
            complaint.CitizenId
        );

        // 2. Check if reporter is vulnerable
        var isVulnerable = await CheckVulnerableReporterAsync(complaint.CitizenId);

        // 3. Determine priority
        var priority = await DetermineAutoPriorityAsync(severityScore, isVulnerable);

        // 4. Route to department
        var departmentId = await RouteToMostSuitableDepartmentAsync(
            complaint.CategoryId,
            complaint.DepartmentId
        );

        // 5. Generate triage notes
        var triageNotes = await GenerateTriageNotesAsync(
            severityScore,
            isVulnerable,
            priority,
            departmentId
        );

        return new TriageResult
        {
            SeverityScore = severityScore,
            Priority = priority,
            DepartmentId = departmentId,
            TriageNotes = triageNotes,
            IsVulnerable = isVulnerable
        };
    }

    public async Task<int> CalculateSeverityScoreAsync(string title, string description, int categoryId, int citizenId)
    {
        int score = 30; // Base score

        var combinedText = $"{title} {description}".ToLower();

        // Keyword-based scoring
        foreach (var keyword in _urgentKeywords)
        {
            if (combinedText.Contains(keyword))
            {
                score += 20; // Each urgent keyword adds significant weight
                break; // Only count once for urgent category
            }
        }

        foreach (var keyword in _highKeywords)
        {
            if (combinedText.Contains(keyword))
            {
                score += 10;
                break;
            }
        }

        foreach (var keyword in _moderateKeywords)
        {
            if (combinedText.Contains(keyword))
            {
                score += 5;
                break;
            }
        }

        // Category risk level adjustment
        var category = await _context.Categories.FindAsync(categoryId);
        if (category != null)
        {
            score += category.RiskLevel switch
            {
                "High" => 15,
                "Medium" => 5,
                "Low" => 0,
                _ => 5
            };
        }

        // Historical complaint pattern (repeat complainant)
        var citizenComplaintCount = await _context.Complaints
            .CountAsync(c => c.CitizenId == citizenId && c.Status != "Resolved");
        
        if (citizenComplaintCount > 3)
        {
            score += 10; // Multiple unresolved complaints suggests persistent issue
        }

        // Cap score at 100
        return Math.Min(score, 100);
    }

    public Task<string> DetermineAutoPriorityAsync(int severityScore, bool isVulnerable)
    {
        // Vulnerable reporters get priority boost
        if (isVulnerable)
        {
            if (severityScore >= 50) return Task.FromResult("High");
            if (severityScore >= 30) return Task.FromResult("Medium");
            return Task.FromResult("Medium"); // Even low-severity complaints from vulnerable citizens get Medium
        }

        // Standard priority determination
        if (severityScore >= 70) return Task.FromResult("High");
        if (severityScore >= 40) return Task.FromResult("Medium");
        return Task.FromResult("Low");
    }

    public async Task<int?> RouteToMostSuitableDepartmentAsync(int categoryId, int? currentDepartmentId)
    {
        // If already assigned to a department, keep it
        if (currentDepartmentId.HasValue)
        {
            return currentDepartmentId;
        }

        // Get category's default department
        var category = await _context.Categories
            .Include(c => c.DefaultDepartment)
            .FirstOrDefaultAsync(c => c.CategoryId == categoryId);

        if (category?.DefaultDepartmentId == null)
        {
            return null; // No default department configured
        }

        var defaultDeptId = category.DefaultDepartmentId.Value;

        // Load balancing: check if default department is overloaded
        var departmentWorkload = await _context.Complaints
            .Where(c => c.DepartmentId == defaultDeptId && c.Status != "Resolved")
            .CountAsync();

        // If default department has too many open complaints, try alternate departments
        if (departmentWorkload > 50)
        {
            // Find departments with active staff that have lower workload
            var alternateDept = await _context.Departments
                .Where(d => d.Staff.Any(s => s.IsActive))
                .Select(d => new
                {
                    d.DepartmentId,
                    Workload = d.Complaints.Count(c => c.Status != "Resolved")
                })
                .OrderBy(d => d.Workload)
                .FirstOrDefaultAsync();

            if (alternateDept != null && alternateDept.Workload < departmentWorkload * 0.7)
            {
                return alternateDept.DepartmentId;
            }
        }

        return defaultDeptId;
    }

    public async Task<bool> CheckVulnerableReporterAsync(int citizenId)
    {
        var citizen = await _context.Citizens.FindAsync(citizenId);
        if (citizen == null) return false;

        // Admin-flagged vulnerable
        if (citizen.IsVulnerable) return true;

        // Disability flag
        if (citizen.HasDisability) return true;

        // Age-based (65+)
        if (citizen.DateOfBirth.HasValue)
        {
            var age = DateTime.Now.Year - citizen.DateOfBirth.Value.Year;
            if (age >= 65) return true;
        }

        return false;
    }

    public async Task<string> GenerateTriageNotesAsync(int severityScore, bool isVulnerable, string priority, int? departmentId)
    {
        var notes = new StringBuilder();

        notes.Append($"Auto-triaged with severity score: {severityScore}/100. ");
        notes.Append($"Priority set to: {priority}. ");

        if (isVulnerable)
        {
            notes.Append("Reporter flagged as vulnerable - priority elevated. ");
        }

        if (departmentId.HasValue)
        {
            var dept = await _context.Departments.FindAsync(departmentId.Value);
            if (dept != null)
            {
                notes.Append($"Routed to: {dept.DepartmentName}. ");
            }
        }
        else
        {
            notes.Append("No department routing configured. ");
        }

        return notes.ToString().Trim();
    }
}
