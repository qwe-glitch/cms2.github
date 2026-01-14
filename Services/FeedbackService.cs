using ComplaintManagementSystem.Models;
using ComplaintManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagementSystem.Services;

/// <summary>
/// Implementation of feedback-related operations
/// </summary>
public class FeedbackService : IFeedbackService
{
    private readonly DB _context;

    public FeedbackService(DB context)
    {
        _context = context;
    }

    // DFD 7.0 - Submit Feedback
    public async Task<bool> SubmitFeedbackAsync(FeedbackViewModel model, int citizenId)
    {
        // Check if complaint exists and belongs to citizen
        var complaint = await _context.Complaints
            .FirstOrDefaultAsync(c => c.ComplaintId == model.ComplaintId && c.CitizenId == citizenId);

        if (complaint == null) return false;

        // Check if complaint is resolved
        if (complaint.Status != "Resolved" )
        {
            return false; // Can only give feedback on resolved/closed complaints
        }

        // Check if feedback already exists
        var existingFeedback = await _context.Feedbacks
            .FirstOrDefaultAsync(f => f.ComplaintId == model.ComplaintId && f.CitizenId == citizenId);

        if (existingFeedback != null) return false; // Already submitted feedback

        var feedback = new Feedback
        {
            Comment = model.Comment,
            Rating = model.Rating,
            CreatedAt = DateTime.Now,
            ComplaintId = model.ComplaintId,
            CitizenId = citizenId
        };

        _context.Feedbacks.Add(feedback);
        await _context.SaveChangesAsync();

        return true;
    }

    // Helper: Get feedback for a complaint
    public async Task<IEnumerable<object>> GetFeedbackByComplaintAsync(int complaintId)
    {
        return await _context.Feedbacks
            .Where(f => f.ComplaintId == complaintId)
            .Include(f => f.Citizen)
            .Select(f => new
            {
                f.FeedbackId,
                f.Comment,
                f.Rating,
                f.CreatedAt,
                CitizenName = f.Citizen!.Name
            })
            .ToListAsync();
    }

    // Helper: Get average rating
    public async Task<double> GetAverageRatingAsync(int complaintId)
    {
        var feedbacks = await _context.Feedbacks
            .Where(f => f.ComplaintId == complaintId)
            .ToListAsync();

        if (!feedbacks.Any()) return 0;

        return feedbacks.Where(f => f.Rating.HasValue).Average(f => f.Rating!.Value);
    }
}
