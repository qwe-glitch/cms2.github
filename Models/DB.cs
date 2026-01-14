using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ComplaintManagementSystem.Models;

#nullable disable warnings

public class DB : DbContext
{
    public DB(DbContextOptions options) : base(options) { }

    public DbSet<Department> Departments { get; set; }
    public DbSet<Staff> Staff { get; set; }
    public DbSet<Citizen> Citizens { get; set; }
    public DbSet<Admin> Admins { get; set; }
    public DbSet<Category> Categories { get; set; }
    public DbSet<Complaint> Complaints { get; set; }
    public DbSet<Attachment> Attachments { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<ComplaintHistory> ComplaintHistories { get; set; }
    public DbSet<Feedback> Feedbacks { get; set; }
    public DbSet<ComplaintLink> ComplaintLinks { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Complaint>()
            .HasOne(x => x.Citizen)
            .WithMany(x => x.Complaints)
            .HasForeignKey(x => x.CitizenId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Feedback>()
            .HasOne(x => x.Citizen)
            .WithMany(x => x.Feedbacks)
            .HasForeignKey(x => x.CitizenId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Feedback>()
            .HasOne(x => x.Complaint)
            .WithMany(x => x.Feedbacks)
            .HasForeignKey(x => x.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Notification>()
            .HasOne(x => x.Citizen)
            .WithMany(x => x.Notifications)
            .HasForeignKey(x => x.CitizenId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasOne(x => x.Staff)
            .WithMany()
            .HasForeignKey(x => x.StaffId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasOne(x => x.Admin)
            .WithMany()
            .HasForeignKey(x => x.AdminId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Notification>()
            .HasOne(x => x.Complaint)
            .WithMany(x => x.Notifications)
            .HasForeignKey(x => x.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Attachment>()
            .HasOne(x => x.Complaint)
            .WithMany(x => x.Attachments)
            .HasForeignKey(x => x.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ComplaintHistory>()
            .HasOne(x => x.Complaint)
            .WithMany(x => x.ComplaintHistories)
            .HasForeignKey(x => x.ComplaintId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ComplaintLink>()
            .HasOne(x => x.SourceComplaint)
            .WithMany(x => x.OutgoingLinks)
            .HasForeignKey(x => x.SourceComplaintId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ComplaintLink>()
            .HasOne(x => x.TargetComplaint)
            .WithMany(x => x.IncomingLinks)
            .HasForeignKey(x => x.TargetComplaintId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

// -----------------------------------------------------------------------------
// Entity Classes
// -----------------------------------------------------------------------------

public class Department
{
    [Key]
    public int DepartmentId { get; set; }

    [Required, StringLength(100)]
    public string DepartmentName { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Location { get; set; }

    public string? Description { get; set; }

    [StringLength(20)]
    public string? OfficePhone { get; set; }

    [StringLength(100)]
    public string? OfficeEmail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Staff> Staff { get; set; } = [];
    public ICollection<Complaint> Complaints { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
    public bool IsActive { get; set; } = true;
}

public class Staff
{
    [Key]
    public int StaffId { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public int DepartmentId { get; set; }

    [ForeignKey("DepartmentId")]
    public Department Department { get; set; } = null!;
    public ICollection<Complaint> Complaints { get; set; } = [];
}

public class Citizen
{
    [Key]
    public int CitizenId { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Phone { get; set; }

    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    
    // Security fields
    public int FailedLoginAttempts { get; set; } = 0;
    public bool IsLocked { get; set; } = false;

    public ICollection<Complaint> Complaints { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<ComplaintHistory> ComplaintHistories { get; set; } = [];
    public ICollection<Feedback> Feedbacks { get; set; } = [];

    // Email verification fields
    public bool IsEmailVerified { get; set; } = false;
    public string? EmailVerificationToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }

    // SSO fields
    public string? ExternalProvider { get; set; } // "Google", "Microsoft", null for regular
    public string? ExternalId { get; set; } // OAuth provider's user ID
    
    // Vulnerable reporter fields (for triage)
    public bool IsVulnerable { get; set; } = false; // Admin-flagged vulnerable citizen
    public DateTime? DateOfBirth { get; set; } // For age-based vulnerability assessment
    public bool HasDisability { get; set; } = false; // Disability flag
}

public class Admin
{
    [Key]
    public int AdminId { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<Complaint> Complaints { get; set; } = [];    
}

public class Category
{
    [Key]
    public int CategoryId { get; set; }

    [Required, StringLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public string? Description { get; set; }
    
    // Triage and routing fields
    public int? DefaultDepartmentId { get; set; } // Default department for this category
    
    [StringLength(20)]
    public string RiskLevel { get; set; } = "Medium"; // Low, Medium, High
    
    // SLA tracking fields
    public int SlaTargetHours { get; set; } = 72; // Default 72 hours (3 days)
    
    [ForeignKey("DefaultDepartmentId")]
    public Department? DefaultDepartment { get; set; }

    public ICollection<Complaint> Complaints { get; set; } = [];
    public bool IsActive { get; set; } = true;
}

public class Complaint
{
    [Key]
    public int ComplaintId { get; set; }

    [Required, StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Location { get; set; }

    [Required, StringLength(50)]
    public string Status { get; set; } = "Pending";

    [Required, StringLength(20)]
    public string Priority { get; set; } = "Medium";

    public bool IsAnonymous { get; set; } = false;

    [StringLength(20)]
    public string? ContactPreference { get; set; } // "Email", "Phone", "None"

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    
    // SLA tracking fields
    public DateTime? SlaDueDate { get; set; } // Calculated based on category SLA
    public DateTime? FirstResponseTime { get; set; } // When staff first responded
    public DateTime? ResolutionTime { get; set; } // When marked as resolved
    
    // Triage and routing fields
    public int SeverityScore { get; set; } = 0; // 0-100 severity score
    public bool AutoAssigned { get; set; } = false; // Whether auto-routed by triage
    public string? TriageNotes { get; set; } // Auto-generated triage notes
    public int? DepartmentId { get; set; } // Routed department

    public int CitizenId { get; set; }
    public int? StaffId { get; set; }
    public int? AdminId { get; set; }
    public int CategoryId { get; set; }

    [ForeignKey("CitizenId")]
    public Citizen Citizen { get; set; } = null!;
    [ForeignKey("StaffId")]
    public Staff? Staff { get; set; }
    [ForeignKey("AdminId")]
    public Admin? Admin { get; set; }
    [ForeignKey("CategoryId")]
    public Category Category { get; set; } = null!;
    [ForeignKey("DepartmentId")]
    public Department? Department { get; set; }

    public ICollection<Attachment> Attachments { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
    public ICollection<ComplaintHistory> ComplaintHistories { get; set; } = [];
    public ICollection<Feedback> Feedbacks { get; set; } = [];
    public ICollection<ComplaintLink> OutgoingLinks { get; set; } = [];
    public ICollection<ComplaintLink> IncomingLinks { get; set; } = [];
}

public class Attachment
{
    [Key]
    public int AttachmentsId { get; set; }

    [Required]
    public string FilePath { get; set; } = string.Empty;

    [StringLength(50)]
    public string? FileType { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int ComplaintId { get; set; }

    [ForeignKey("ComplaintId")]
    public Complaint Complaint { get; set; } = null!;
}

public class Notification
{
    [Key]
    public int NotificationId { get; set; }

    [Required]
    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public int ComplaintId { get; set; }
    
    // Make all recipient IDs nullable - only one should be set
    public int? CitizenId { get; set; }
    public int? StaffId { get; set; }
    public int? AdminId { get; set; }

    [ForeignKey("ComplaintId")]
    public Complaint Complaint { get; set; } = null!;
    [ForeignKey("CitizenId")]
    public Citizen? Citizen { get; set; }
    [ForeignKey("StaffId")]
    public Staff? Staff { get; set; }
    [ForeignKey("AdminId")]
    public Admin? Admin { get; set; }
}

public class ComplaintHistory
{
    [Key]
    public int HistoryId { get; set; }

    [StringLength(50)]
    public string? StatusBefore { get; set; }

    [StringLength(50)]
    public string? StatusAfter { get; set; }

    [StringLength(100)]
    public string? ChangeBy { get; set; }

    public DateTime ChangeTime { get; set; } = DateTime.UtcNow;

    [StringLength(1000)]
    public string? Notes { get; set; } // Update notes for this status change

    public int ComplaintId { get; set; }
    public int? CitizenId { get; set; }

    [ForeignKey("ComplaintId")]
    public Complaint Complaint { get; set; } = null!;
    [ForeignKey("CitizenId")]
    public Citizen? Citizen { get; set; }
}

public class Feedback
{
    [Key]
    public int FeedbackId { get; set; }

    public string? Comment { get; set; }

    [Range(1, 5)]
    public int? Rating { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int ComplaintId { get; set; }
    public int CitizenId { get; set; }

    [ForeignKey("ComplaintId")]
    public Complaint Complaint { get; set; } = null!;
    [ForeignKey("CitizenId")]
    public Citizen Citizen { get; set; } = null!;
}

public class ComplaintLink
{
    [Key]
    public int LinkId { get; set; }

    public int SourceComplaintId { get; set; }
    public int TargetComplaintId { get; set; }

    [Required, StringLength(20)]
    public string LinkType { get; set; } = "Related"; // "Duplicate", "Related", "FollowUp"

    public double? SimilarityScore { get; set; } // 0-100 for duplicates

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedByUserId { get; set; }
    
    [StringLength(20)]
    public string CreatedByUserType { get; set; } = string.Empty; // "Admin", "Staff"

    [ForeignKey("SourceComplaintId")]
    public Complaint SourceComplaint { get; set; } = null!;
    
    [ForeignKey("TargetComplaintId")]
    public Complaint TargetComplaint { get; set; } = null!;
}
