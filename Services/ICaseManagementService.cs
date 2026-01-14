using ComplaintManagementSystem.Models.ViewModels;

namespace ComplaintManagementSystem.Services;

/// <summary>
/// Interface for case management operations including duplicate detection and complaint linking
/// </summary>
public interface ICaseManagementService
{
    /// <summary>
    /// Finds potential duplicate complaints for a given complaint
    /// </summary>
    Task<DuplicateDetectionViewModel> FindPotentialDuplicatesAsync(int complaintId);
    
    /// <summary>
    /// Links two complaints together
    /// </summary>
    Task<bool> LinkComplaintsAsync(int sourceComplaintId, int targetComplaintId, string linkType, string? notes, int userId, string userType);
    
    /// <summary>
    /// Removes a link between complaints
    /// </summary>
    Task<bool> UnlinkComplaintsAsync(int linkId);
    
    /// <summary>
    /// Gets all complaints linked to a specific complaint
    /// </summary>
    Task<List<LinkedComplaintViewModel>> GetLinkedComplaintsAsync(int complaintId);
    
    /// <summary>
    /// Marks a complaint as duplicate and auto-closes it
    /// </summary>
    Task<bool> MarkAsDuplicateAsync(int originalComplaintId, int duplicateComplaintId, int userId, string userType);
    
    /// <summary>
    /// Checks if two complaints are already linked
    /// </summary>
    Task<bool> AreComplaintsLinkedAsync(int complaintId1, int complaintId2);
}
