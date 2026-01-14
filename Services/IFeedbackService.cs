using ComplaintManagementSystem.Models.ViewModels;

namespace ComplaintManagementSystem.Services;

/// <summary>
/// Interface for feedback-related operations
/// </summary>
public interface IFeedbackService
{
    // DFD 7.0 - Submit Feedback
    Task<bool> SubmitFeedbackAsync(FeedbackViewModel model, int citizenId);

    // Helper methods
    Task<IEnumerable<object>> GetFeedbackByComplaintAsync(int complaintId);
    Task<double> GetAverageRatingAsync(int complaintId);
}
