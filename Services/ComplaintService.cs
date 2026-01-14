using ComplaintManagementSystem.Models;
using ComplaintManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagementSystem.Services;

/// <summary>
/// Implementation of complaint-related operations
/// </summary>
public class ComplaintService : IComplaintService
{
    private readonly DB _context;
    private readonly IWebHostEnvironment _environment;
    private readonly INotificationService _notificationService;
    private readonly ITriageService _triageService;

    public ComplaintService(DB context, IWebHostEnvironment environment, INotificationService notificationService, ITriageService triageService)
    {
        _context = context;
        _environment = environment;
        _notificationService = notificationService;
        _triageService = triageService;
    }

    // DFD 1.0 - Submit Complaint
    public async Task<int> SubmitComplaintAsync(ComplaintSubmissionViewModel model, int citizenId)
    {
        var complaint = new Complaint
        {
            Title = model.Title,
            Description = model.Description,
            Location = model.Location,
            CategoryId = model.CategoryId,
            Priority = model.Priority,
            IsAnonymous = model.IsAnonymous,
            ContactPreference = model.ContactPreference,
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            Status = "Pending",
            CitizenId = citizenId,
            SubmittedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Calculate SLA Due Date
        var category = await _context.Categories.FindAsync(model.CategoryId);
        if (category != null)
        {
            complaint.SlaDueDate = DateTime.Now.AddHours(category.SlaTargetHours);
        }

        _context.Complaints.Add(complaint);
        await _context.SaveChangesAsync();

        // Upload attachments if any
        if (model.Attachments != null && model.Attachments.Any())
        {
            await UploadAttachmentsAsync(complaint.ComplaintId, model.Attachments);
        }

        // Perform automatic triage assessment
        try
        {
            var triageResult = await _triageService.AssessComplaintAsync(complaint.ComplaintId);
            
            // Update complaint with triage results
            complaint.SeverityScore = triageResult.SeverityScore;
            complaint.Priority = triageResult.Priority; // Override with auto-determined priority
            complaint.DepartmentId = triageResult.DepartmentId;
            complaint.TriageNotes = triageResult.TriageNotes;
            complaint.AutoAssigned = triageResult.DepartmentId.HasValue;
            complaint.UpdatedAt = DateTime.Now;
            
            await _context.SaveChangesAsync();
        }
        catch (Exception)
        {
            // If triage fails, continue with manual processing
            // The complaint is still created, just without automatic triage
        }

        // Send notification to admin about new complaint with triage info
        var complaintWithTriage = await _context.Complaints
            .Include(c => c.Department)
            .FirstOrDefaultAsync(c => c.ComplaintId == complaint.ComplaintId);
            
        var notificationMessage = $"New complaint submitted: {complaint.Title} " +
            $"[Priority: {complaintWithTriage?.Priority ?? complaint.Priority}, " +
            $"Severity: {complaintWithTriage?.SeverityScore ?? 0}/100]";
        
        var firstAdmin = await _context.Admins.FirstOrDefaultAsync();
        if (firstAdmin != null)
        {
            await _notificationService.SendNotificationAsync(
                notificationMessage,
                complaint.ComplaintId,
                null, // citizenId
                null, // staffId
                firstAdmin.AdminId // adminId
            );
        }

        return complaint.ComplaintId;
    }

    // DFD 2.0 - Upload Attachment
    public async Task<bool> UploadAttachmentsAsync(int complaintId, List<IFormFile> files)
    {
        try
        {
            var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "complaints");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    var fileName = $"{complaintId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(uploadPath, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var attachment = new Attachment
                    {
                        FilePath = $"/uploads/complaints/{fileName}",
                        FileType = file.ContentType,
                        UploadedAt = DateTime.Now,
                        ComplaintId = complaintId
                    };

                    _context.Attachments.Add(attachment);
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    // DFD 3.0 - Update Complaint
    public async Task<bool> UpdateComplaintAsync(ComplaintUpdateViewModel model, int userId)
    {
        var complaint = await _context.Complaints.FindAsync(model.ComplaintId);
        if (complaint == null) return false;

        var oldStatus = complaint.Status;
        var oldStaffId = complaint.StaffId;

        // Update complaint fields
        complaint.Status = model.Status;
        complaint.UpdatedAt = DateTime.Now;

        if (model.AssignedStaffId.HasValue)
        {
            complaint.StaffId = model.AssignedStaffId.Value;
        }

        if (!string.IsNullOrEmpty(model.Priority))
        {
            complaint.Priority = model.Priority;
        }

        if (model.Status == "Resolved" )
        {
            if (!complaint.ResolvedAt.HasValue)
            {
                complaint.ResolvedAt = DateTime.Now;
                complaint.ResolutionTime = DateTime.Now;
            }
        }
        
        // Track First Response Time
        if (model.Status == "In Progress" && !complaint.FirstResponseTime.HasValue)
        {
            complaint.FirstResponseTime = DateTime.Now;
        }

        await _context.SaveChangesAsync();

        // Log status change
        await LogStatusChangeAsync(model.ComplaintId, oldStatus, model.Status, userId, model.UpdateNotes);

        // Notify citizen about status change
        await _notificationService.SendNotificationAsync(
            $"Your complaint status has been updated to: {model.Status}",
            complaint.ComplaintId,
            complaint.CitizenId,
            null,
            null
        );

        // Notify staff if newly assigned
        if (model.AssignedStaffId.HasValue && oldStaffId != model.AssignedStaffId.Value)
        {
            await _notificationService.SendNotificationAsync(
                $"You have been assigned to complaint: {complaint.Title}",
                complaint.ComplaintId,
                null,
                model.AssignedStaffId.Value,
                null
            );
        }

        return true;
    }

    // DFD 4.0 - Log Status Change
    public async Task LogStatusChangeAsync(int complaintId, string statusBefore, string statusAfter, int changedBy, string? notes = null)
    {
        var history = new ComplaintHistory
        {
            StatusBefore = statusBefore,
            StatusAfter = statusAfter,
            ChangeTime = DateTime.Now,
            ComplaintId = complaintId,
            ChangeBy = changedBy.ToString(), // Convert int to string
            Notes = notes
        };

        _context.ComplaintHistories.Add(history);
        await _context.SaveChangesAsync();
    }

    // Helper methods
    public async Task<ComplaintUpdateViewModel?> GetComplaintForUpdateAsync(int complaintId)
    {
        var complaint = await _context.Complaints.FindAsync(complaintId);
        if (complaint == null) return null;

        return new ComplaintUpdateViewModel
        {
            ComplaintId = complaint.ComplaintId,
            Status = complaint.Status,
            AssignedStaffId = complaint.StaffId,
            Priority = complaint.Priority
        };
    }

    public async Task<IEnumerable<object>> GetComplaintsByCitizenAsync(int citizenId)
    {
        return await _context.Complaints
            .Where(c => c.CitizenId == citizenId)
            .Include(c => c.Category)
            .OrderByDescending(c => c.SubmittedAt)
            .Select(c => new
            {
                c.ComplaintId,
                c.Title,
                c.Status,
                c.Priority,
                CategoryName = c.Category!.CategoryName,
                c.SubmittedAt,
                c.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<object>> GetComplaintsByStaffAsync(int staffId)
    {
        return await _context.Complaints
            .Where(c => c.StaffId == staffId)
            .Include(c => c.Category)
            .Include(c => c.Citizen)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new
            {
                c.ComplaintId,
                c.Title,
                c.Status,
                c.Priority,
                CategoryName = c.Category!.CategoryName,
                CitizenName = c.IsAnonymous ? "Anonymous" : c.Citizen!.Name,
                c.SubmittedAt,
                c.UpdatedAt
            })
            .ToListAsync();
    }

    // Staff work view - Get resolved complaints
    public async Task<IEnumerable<object>> GetStaffResolvedComplaintsAsync(int staffId)
    {
        return await _context.Complaints
            .Where(c => c.StaffId == staffId && (c.Status == "Resolved" || c.Status == "Closed" || c.Status == "Rejected"))
            .Include(c => c.Category)
            .Include(c => c.Citizen)
            .OrderByDescending(c => c.ResolvedAt ?? c.UpdatedAt)
            .Select(c => new
            {
                c.ComplaintId,
                c.Title,
                c.Status,
                c.Priority,
                CategoryName = c.Category!.CategoryName,
                CitizenName = c.IsAnonymous ? "Anonymous" : c.Citizen!.Name,
                c.SubmittedAt,
                c.UpdatedAt,
                c.ResolvedAt
            })
            .ToListAsync();
    }

    // Staff work view - Get in-progress complaints
    public async Task<IEnumerable<object>> GetStaffInProgressComplaintsAsync(int staffId)
    {
        return await _context.Complaints
            .Where(c => c.StaffId == staffId && c.Status == "In Progress")
            .Include(c => c.Category)
            .Include(c => c.Citizen)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new
            {
                c.ComplaintId,
                c.Title,
                c.Status,
                c.Priority,
                CategoryName = c.Category!.CategoryName,
                CitizenName = c.IsAnonymous ? "Anonymous" : c.Citizen!.Name,
                c.SubmittedAt,
                c.UpdatedAt
            })
            .ToListAsync();
    }

    // Staff work view - Get pending complaints
    public async Task<IEnumerable<object>> GetStaffPendingComplaintsAsync(int staffId)
    {
        return await _context.Complaints
            .Where(c => c.StaffId == staffId && c.Status == "Pending")
            .Include(c => c.Category)
            .Include(c => c.Citizen)
            .OrderByDescending(c => c.SubmittedAt)
            .Select(c => new
            {
                c.ComplaintId,
                c.Title,
                c.Status,
                c.Priority,
                CategoryName = c.Category!.CategoryName,
                CitizenName = c.IsAnonymous ? "Anonymous" : c.Citizen!.Name,
                c.SubmittedAt,
                c.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<object>> GetAllComplaintsAsync()
    {
        return await _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.Citizen)
            .Include(c => c.Staff)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new
            {
                c.ComplaintId,
                c.Title,
                c.Status,
                c.Priority,
                CategoryName = c.Category!.CategoryName,
                CitizenName = c.IsAnonymous ? "Anonymous" : c.Citizen!.Name,
                StaffName = c.Staff != null ? c.Staff.Name : "Unassigned",
                c.SubmittedAt,
                c.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<object?> GetComplaintDetailsAsync(int complaintId)
    {
        return await _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.Citizen)
            .Include(c => c.Staff)
            .Include(c => c.Attachments)
            .Include(c => c.ComplaintHistories)
            .Include(c => c.Feedbacks)
            .Where(c => c.ComplaintId == complaintId)
            .Select(c => new
            {
                c.ComplaintId,
                c.Title,
                c.Description,
                c.Location,
                c.Latitude,
                c.Longitude,
                c.Status,
                c.Priority,
                CategoryName = c.Category!.CategoryName,
                CitizenName = c.IsAnonymous ? "Anonymous" : c.Citizen!.Name,
                CitizenEmail = c.IsAnonymous ? "Hidden" : c.Citizen.Email,
                StaffName = c.Staff != null ? c.Staff.Name : "Unassigned",
                c.SubmittedAt,
                c.UpdatedAt,
                c.ResolvedAt,
                Attachments = c.Attachments.Select(a => new { a.AttachmentsId, a.FilePath, a.FileType }),
                History = c.ComplaintHistories.Select(h => new { h.StatusBefore, h.StatusAfter, h.ChangeTime, h.Notes }),
                Feedback = c.Feedbacks.Select(f => new { f.Comment, f.Rating, f.CreatedAt })
            })
            .FirstOrDefaultAsync();
    }

    public async Task<object?> GetComplaintDetailsAsync(int complaintId, int? currentUserId, string? userType)
    {
        var complaint = await _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.Citizen)
            .Include(c => c.Staff)
            .Include(c => c.Attachments)
            .Include(c => c.ComplaintHistories)
            .Include(c => c.Feedbacks)
            .Where(c => c.ComplaintId == complaintId)
            .FirstOrDefaultAsync();

        if (complaint == null) return null;

        // Determine if citizen info should be masked
        // Mask citizen info if:
        // 1. The complaint is anonymous OR
        // 2. Current user is a Citizen AND they are not the owner of this complaint
        bool maskCitizenInfo = complaint.IsAnonymous;
        
        if (!maskCitizenInfo && userType == "Citizen" && currentUserId.HasValue)
        {
            // If current user is a citizen viewing another citizen's complaint
            if (complaint.CitizenId != currentUserId.Value)
            {
                maskCitizenInfo = true;
            }
        }

        string citizenName = maskCitizenInfo ? "Protected" : complaint.Citizen?.Name ?? "Unknown";
        string citizenEmail = maskCitizenInfo ? "Protected" : complaint.Citizen?.Email ?? "Hidden";

        return new
        {
            complaint.ComplaintId,
            complaint.Title,
            complaint.Description,
            complaint.Location,
            complaint.Latitude,
            complaint.Longitude,
            complaint.Status,
            complaint.Priority,
            CategoryName = complaint.Category!.CategoryName,
            CitizenName = citizenName,
            CitizenEmail = citizenEmail,
            StaffName = complaint.Staff != null ? complaint.Staff.Name : "Unassigned",
            complaint.SubmittedAt,
            complaint.UpdatedAt,
            complaint.ResolvedAt,
            Attachments = complaint.Attachments.Select(a => new { a.AttachmentsId, a.FilePath, a.FileType }),
            History = complaint.ComplaintHistories.Select(h => new { h.StatusBefore, h.StatusAfter, h.ChangeTime, h.Notes }),
            Feedback = complaint.Feedbacks.Select(f => new { f.Comment, f.Rating, f.CreatedAt })
        };
    }

    public async Task<IEnumerable<object>> GetPublicComplaintsAsync()
    {
        // Only show Pending, In Progress, and Resolved complaints on the public board
        var allowedStatuses = new[] { "Pending", "In Progress", "Resolved" };
        
        return await _context.Complaints
            .Where(c => allowedStatuses.Contains(c.Status))
            .Include(c => c.Category)
            .Include(c => c.Attachments)
            .OrderByDescending(c => c.SubmittedAt)
            .Select(c => new
            {
                c.ComplaintId,
                c.Title,
                c.Description,
                c.Location,
                c.Status,
                c.Priority,
                CategoryName = c.Category!.CategoryName,
                c.SubmittedAt,
                c.UpdatedAt,
                Attachments = c.Attachments.Select(a => new { a.FilePath, a.FileType }).ToList()
            })
            .ToListAsync();
    }

    // Delete complaint (admin only)
    public async Task<bool> DeleteComplaintAsync(int complaintId)
    {
        try
        {
            var complaint = await _context.Complaints
                .Include(c => c.Attachments)
                .Include(c => c.OutgoingLinks)
                .Include(c => c.IncomingLinks)
                .Include(c => c.ComplaintHistories)
                .Include(c => c.Notifications)
                .Include(c => c.Feedbacks)
                .FirstOrDefaultAsync(c => c.ComplaintId == complaintId);
            
            if (complaint == null)
            {
                return false;
            }

            var webRoot = _environment?.WebRootPath;
            if (!string.IsNullOrWhiteSpace(webRoot) && Directory.Exists(webRoot))
            {
                foreach (var attachment in complaint.Attachments)
                {
                    if (!string.IsNullOrEmpty(attachment.FilePath))
                    {
                        var physicalPath = Path.Combine(webRoot, attachment.FilePath.TrimStart('/'));
                        if (File.Exists(physicalPath))
                        {
                            File.Delete(physicalPath);
                        }
                    }
                }
            }

            // Manually delete related entities to avoid cascade issues
            if (complaint.Notifications.Any())
            {
                _context.Notifications.RemoveRange(complaint.Notifications);
            }
            if (complaint.ComplaintHistories.Any())
            {
                _context.ComplaintHistories.RemoveRange(complaint.ComplaintHistories);
            }
            if (complaint.Feedbacks.Any())
            {
                _context.Feedbacks.RemoveRange(complaint.Feedbacks);
            }
            if (complaint.Attachments.Any())
            {
                _context.Attachments.RemoveRange(complaint.Attachments);
            }

            // Manually delete complaint links (due to SQL Server cascade path limitation)
            if (complaint.OutgoingLinks.Any())
            {
                _context.ComplaintLinks.RemoveRange(complaint.OutgoingLinks);
            }
            if (complaint.IncomingLinks.Any())
            {
                _context.ComplaintLinks.RemoveRange(complaint.IncomingLinks);
            }

            // Remove complaint (cascade delete will handle other related records)
            _context.Complaints.Remove(complaint);
            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting complaint: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            return false;
        }
    }


    // DFD 5.0 - Edit Complaint (Citizen)
    public async Task<bool> UpdateComplaintDetailsAsync(ComplaintEditViewModel model, int citizenId)
    {
        var complaint = await _context.Complaints.FindAsync(model.ComplaintId);
        
        // Check if complaint exists, belongs to citizen, and is Pending
        if (complaint == null || complaint.CitizenId != citizenId || complaint.Status != "Pending") 
        {
            return false;
        }

        // Update fields
        complaint.Title = model.Title;
        complaint.Description = model.Description;
        complaint.Location = model.Location;
        complaint.CategoryId = model.CategoryId;
        complaint.Priority = model.Priority;
        complaint.IsAnonymous = model.IsAnonymous;
        complaint.ContactPreference = model.ContactPreference;
        complaint.Latitude = model.Latitude;
        complaint.Longitude = model.Longitude;
        complaint.UpdatedAt = DateTime.Now;

        // Recalculate SLA if category changed
        var category = await _context.Categories.FindAsync(model.CategoryId);
        if (category != null)
        {
            complaint.SlaDueDate = DateTime.Now.AddHours(category.SlaTargetHours);
        }

        await _context.SaveChangesAsync();
        
        // Delete attachments if requested
        if (model.AttachmentsToDelete != null && model.AttachmentsToDelete.Any())
        {
            foreach (var attachmentId in model.AttachmentsToDelete)
            {
                await DeleteAttachmentAsync(attachmentId);
            }
        }

        // Upload new attachments if any
        if (model.Attachments != null && model.Attachments.Any())
        {
            await UploadAttachmentsAsync(complaint.ComplaintId, model.Attachments);
        }

        return true;
    }

    public async Task<ComplaintEditViewModel?> GetComplaintForEditAsync(int complaintId, int citizenId)
    {
        var complaint = await _context.Complaints
            .Include(c => c.Attachments)
            .FirstOrDefaultAsync(c => c.ComplaintId == complaintId);
        
        // Check if complaint exists, belongs to citizen, and is Pending
        if (complaint == null || complaint.CitizenId != citizenId || complaint.Status != "Pending") 
        {
            return null;
        }

        return new ComplaintEditViewModel
        {
            ComplaintId = complaint.ComplaintId,
            Title = complaint.Title,
            Description = complaint.Description,
            Location = complaint.Location ?? string.Empty,
            CategoryId = complaint.CategoryId,
            Priority = complaint.Priority,
            IsAnonymous = complaint.IsAnonymous,
            ContactPreference = complaint.ContactPreference,
            ExistingAttachments = complaint.Attachments.Select(a => new ExistingAttachmentViewModel
            {
                AttachmentId = a.AttachmentsId,
                FilePath = a.FilePath,
                FileName = Path.GetFileName(a.FilePath),
                FileType = a.FileType
            }).ToList()
        };
    }

    public async Task<bool> DeleteAttachmentAsync(int attachmentId)
    {
        try
        {
            var attachment = await _context.Attachments.FindAsync(attachmentId);
            if (attachment == null) return false;

            // Delete physical file
            var webRoot = _environment.WebRootPath;
            var physicalPath = Path.Combine(webRoot, attachment.FilePath.TrimStart('/'));
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }

            // Remove database record
            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting attachment {attachmentId}: {ex.Message}");
            return false;
        }
    }
}

