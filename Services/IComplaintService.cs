using ComplaintManagementSystem.Models.ViewModels;

namespace ComplaintManagementSystem.Services;

/// <summary>
/// Interface for complaint-related operations
/// </summary>
public interface IComplaintService
{
    // DFD 1.0 - Submit Complaint
    Task<int> SubmitComplaintAsync(ComplaintSubmissionViewModel model, int citizenId);

    // DFD 2.0 - Upload Attachment
    Task<bool> UploadAttachmentsAsync(int complaintId, List<IFormFile> files);

    // DFD 3.0 - Update Complaint
    Task<bool> UpdateComplaintAsync(ComplaintUpdateViewModel model, int userId);

    // DFD 4.0 - Log Status Change
    Task LogStatusChangeAsync(int complaintId, string statusBefore, string statusAfter, int changedBy, string? notes = null);

    // Additional helper methods
    Task<ComplaintUpdateViewModel?> GetComplaintForUpdateAsync(int complaintId);
    Task<IEnumerable<object>> GetComplaintsByCitizenAsync(int citizenId);
    Task<IEnumerable<object>> GetComplaintsByStaffAsync(int staffId);
    
    // Staff work view - filter by status
    Task<IEnumerable<object>> GetStaffResolvedComplaintsAsync(int staffId);
    Task<IEnumerable<object>> GetStaffInProgressComplaintsAsync(int staffId);
    Task<IEnumerable<object>> GetStaffPendingComplaintsAsync(int staffId);
    Task<IEnumerable<object>> GetAllComplaintsAsync();
    Task<object?> GetComplaintDetailsAsync(int complaintId);
    Task<object?> GetComplaintDetailsAsync(int complaintId, int? currentUserId, string? userType);
    
    // Public complaints (no citizen personal info)
    Task<IEnumerable<object>> GetPublicComplaintsAsync();
    
    // Delete complaint (admin only)
    Task<bool> DeleteComplaintAsync(int complaintId);

    // DFD 5.0 - Edit Complaint (Citizen)
    Task<bool> UpdateComplaintDetailsAsync(ComplaintEditViewModel model, int citizenId);
    Task<ComplaintEditViewModel?> GetComplaintForEditAsync(int complaintId, int citizenId);
    Task<bool> DeleteAttachmentAsync(int attachmentId);
}
