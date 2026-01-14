using ComplaintManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ComplaintManagementSystem.Services
{
    /// <summary>
    /// Serves as a firewall between the AI and the Database.
    /// explicitly filters out sensitive fields and provides high-level summaries.
    /// </summary>
    public class SafeDataService
    {
        private readonly DB _context;

        public SafeDataService(DB context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns a safe, text-based description of the database schema.
        /// EXCLUDES: PasswordHash, EmailVerificationToken, ExternalId, etc.
        /// </summary>
        public string GetSafeSchemaDescription()
        {
            var sb = new StringBuilder();
            sb.AppendLine("SYSTEM DATABASE SCHEMA (READ-ONLY CONTEXT):");
            sb.AppendLine("------------------------------------------------");

            // Departments
            sb.AppendLine("Table: Department");
            sb.AppendLine("- DepartmentId (int, PK)");
            sb.AppendLine("- DepartmentName (string): Name of the municipal department");
            sb.AppendLine("- Location (string): Physical office location");
            sb.AppendLine("- OfficePhone (string): Public contact phone");
            sb.AppendLine("- OfficeEmail (string): Public contact email");
            sb.AppendLine("");

            // Categories
            sb.AppendLine("Table: Category");
            sb.AppendLine("- CategoryId (int, PK)");
            sb.AppendLine("- CategoryName (string): Type of complaint (e.g. Infrastructure, Noise)");
            sb.AppendLine("- SlaTargetHours (int): Target resolution time in hours");
            sb.AppendLine("- RiskLevel (string): 'Low', 'Medium', 'High'");
            sb.AppendLine("");

            // Complaints
            sb.AppendLine("Table: Complaint");
            sb.AppendLine("- ComplaintId (int, PK)");
            sb.AppendLine("- Title (string): Summary of the issue");
            sb.AppendLine("- Description (string): Detailed report");
            sb.AppendLine("- Status (string): 'Pending', 'In Progress', 'Resolved', 'Rejected'");
            sb.AppendLine("- Priority (string): 'Low', 'Medium', 'High', 'Critical'");
            sb.AppendLine("- Location (string): Where the issue occurred");
            sb.AppendLine("- SubmittedAt (DateTime): When it was reported");
            sb.AppendLine("- IsAnonymous (bool): If true, reporter identity is hidden");
            sb.AppendLine("");

            // Citizen (Sanitized)
            sb.AppendLine("Table: Citizen (User)");
            sb.AppendLine("- CitizenId (int, PK)");
            sb.AppendLine("- Name (string): Full name");
            sb.AppendLine("- IsActive (bool): Account status");
            sb.AppendLine("* SENSITIVE FIELDS OMITTED (Passwords, Tokens, etc.) *");
            sb.AppendLine("");

            return sb.ToString();
        }

        /// <summary>
        /// Returns high-level system statistics that are safe to share.
        /// </summary>
        public async Task<string> GetSafeSystemStatsAsync()
        {
            var sb = new StringBuilder();
            
            // Get stats safely
            var totalComplaints = await _context.Complaints.CountAsync();
            var resolvedComplaints = await _context.Complaints.CountAsync(c => c.Status == "Resolved");
            var pendingComplaints = await _context.Complaints.CountAsync(c => c.Status == "Pending");
            
            // Time-based stats
            // Calculate timestamps
            var now = DateTime.Now;
            var today = now.Date;
            var sevenDaysAgo = now.AddDays(-7);
            var thirtyDaysAgo = today.AddDays(-30);
            var thisMonth = new DateTime(now.Year, now.Month, 1);
            
            // Fetch counts
            var newLast7Days = await _context.Complaints.CountAsync(c => c.SubmittedAt >= sevenDaysAgo);
            var newThisMonth = await _context.Complaints.CountAsync(c => c.SubmittedAt >= thisMonth);
            var newToday = await _context.Complaints.CountAsync(c => c.SubmittedAt >= today);

            // Fetch daily breakdown
            var dailyStats = await _context.Complaints
                .Where(c => c.SubmittedAt >= thirtyDaysAgo)
                .GroupBy(c => c.SubmittedAt.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Date)
                .ToListAsync();

            sb.AppendLine("CURRENT SYSTEM STATISTICS:");
            sb.AppendLine($"- Total Complaints Reported: {totalComplaints}");
            sb.AppendLine($"- Resolved: {resolvedComplaints}");
            sb.AppendLine($"- Pending: {pendingComplaints}");
            sb.AppendLine($"- New (Today): {newToday}");
            sb.AppendLine($"- New (Last 7 Days): {newLast7Days}");
            sb.AppendLine($"- New (This Month): {newThisMonth}");

            if (dailyStats.Any())
            {
                sb.AppendLine("- Daily Breakdown (Last 30 Days):");
                foreach (var day in dailyStats)
                {
                    sb.AppendLine($"  - {day.Date:yyyy-MM-dd}: {day.Count}");
                }
            }

            // Department stats
            var deptStats = await _context.Departments
                .Select(d => new { d.DepartmentName, Count = d.Complaints.Count })
                .ToListAsync();

            sb.AppendLine("- Complaints by Department:");
            foreach (var stat in deptStats)
            {
                if (stat.Count > 0)
                    sb.AppendLine($"  - {stat.DepartmentName}: {stat.Count}");
            }

            return sb.ToString();
        }
        /// <summary>
        /// Searches for complaints matching a keyword in title, description, or category.
        /// Returns a safe summary of matches.
        /// </summary>
        public async Task<string> SearchComplaintsAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm)) return string.Empty;

            searchTerm = searchTerm.ToLower();

            var matches = await _context.Complaints
                .Include(c => c.Category)
                .Where(c => c.Title.ToLower().Contains(searchTerm) || 
                            c.Description.ToLower().Contains(searchTerm) ||
                            c.Category.CategoryName.ToLower().Contains(searchTerm))
                .OrderByDescending(c => c.SubmittedAt)
                .Take(5)
                .Select(c => new 
                {
                    c.ComplaintId,
                    c.Title,
                    c.Status,
                    c.SubmittedAt,
                    Category = c.Category.CategoryName
                })
                .ToListAsync();

            if (!matches.Any()) return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine($"FOUND COMPLAINTS MATCHING '{searchTerm}':");
            foreach (var m in matches)
            {
                sb.AppendLine($"- ID {m.ComplaintId}: {m.Title} [{m.Status}] ({m.SubmittedAt:yyyy-MM-dd}) - Cat: {m.Category}");
            }
            return sb.ToString();
        }
    }
}
