using ComplaintManagementSystem.Models;
using ComplaintManagementSystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagementSystem.Services;

/// <summary>
/// Implementation of case management operations
/// </summary>
public class CaseManagementService : ICaseManagementService
{
    private readonly DB _context;
    private readonly INotificationService _notificationService;
    private readonly AiChatService _aiChatService;
    private const double SIMILARITY_THRESHOLD = 70.0;
    private const int DAYS_WINDOW = 7;

    public CaseManagementService(DB context, INotificationService notificationService, AiChatService aiChatService)
    {
        _context = context;
        _notificationService = notificationService;
        _aiChatService = aiChatService;
    }

    public async Task<DuplicateDetectionViewModel> FindPotentialDuplicatesAsync(int complaintId)
    {
        var complaint = await _context.Complaints
            .Include(c => c.Category)
            .FirstOrDefaultAsync(c => c.ComplaintId == complaintId);

        if (complaint == null)
            throw new InvalidOperationException("Complaint not found");

        var viewModel = new DuplicateDetectionViewModel
        {
            OriginalComplaintId = complaint.ComplaintId,
            OriginalTitle = complaint.Title,
            OriginalDescription = complaint.Description,
            OriginalLocation = complaint.Location ?? "",
            OriginalSubmittedAt = complaint.SubmittedAt
        };

        // Get existing links to avoid showing already linked complaints
        var existingLinks = await _context.ComplaintLinks
            .Where(l => l.SourceComplaintId == complaintId || l.TargetComplaintId == complaintId)
            .Select(l => l.SourceComplaintId == complaintId ? l.TargetComplaintId : l.SourceComplaintId)
            .ToListAsync();

        // Get complaints from same category within time window
        var startDate = complaint.SubmittedAt.AddDays(-DAYS_WINDOW);
        var endDate = complaint.SubmittedAt.AddDays(DAYS_WINDOW);

        var candidates = await _context.Complaints
            .Include(c => c.Category)
            .Where(c => c.ComplaintId != complaintId &&
                       // Removed strict category check to catch duplicates across categories
                       c.SubmittedAt >= startDate &&
                       c.SubmittedAt <= endDate)
            .ToListAsync();

        var potentialDuplicates = new List<PotentialDuplicateItem>();

        // 1. First pass: Calculate algorithmic scores for ALL candidates
        var initialCandidates = new List<PotentialDuplicateItem>();

        foreach (var candidate in candidates)
        {
            var score = CalculateSimilarityScore(complaint, candidate);
            
            // Keep potential candidates with loose threshold
            if (score >= 15.0)
            {
                var reason = BuildSimilarityReason(complaint, candidate, score);
                
                initialCandidates.Add(new PotentialDuplicateItem
                {
                    ComplaintId = candidate.ComplaintId,
                    Title = candidate.Title,
                    Description = candidate.Description,
                    Location = candidate.Location ?? "",
                    Status = candidate.Status,
                    CategoryName = candidate.Category?.CategoryName ?? "",
                    SubmittedAt = candidate.SubmittedAt,
                    SimilarityScore = Math.Round(score, 2),
                    SimilarityReason = reason,
                    IsAlreadyLinked = existingLinks.Contains(candidate.ComplaintId)
                });
            }
        }

        // 2. Select top 5 candidates for expensive AI check
        // We prioritize those with highest algorithmic similarity to avoid wasting API calls on weak matches
        var topCandidates = initialCandidates
            .OrderByDescending(x => x.SimilarityScore)
            .Take(5)
            .ToList();

        // 3. Run AI check on top candidates
        foreach (var item in topCandidates)
        {
            try 
            {
                var aiResult = await _aiChatService.AnalyzeDuplicationAsync(
                    complaint.Title, complaint.Description,
                    item.Title, item.Description
                );
                
                item.AiSimilarityScore = aiResult.Score;
                item.AiReasoning = aiResult.Reasoning;
            }
            catch
            {
                // Ignore AI errors
            }
        }

        // 4. Filter final list
        // Show if:
        // - Algo score is very high (>= 70) [Strong Traditional Match]
        // - OR AI score is high (>= 70) [Strong Semantic Match]
        // - OR it was one of the top candidates and Algo score is decent (>= 40) [Moderate Match fallback]
        foreach (var item in initialCandidates)
        {
            bool isHighAlgo = item.SimilarityScore >= 70;
            bool isHighAi = item.AiSimilarityScore.HasValue && item.AiSimilarityScore.Value >= 70;
            bool isModerate = item.SimilarityScore >= 40;

            if (isHighAlgo || isHighAi || (isModerate && item.AiSimilarityScore.HasValue)) 
            {
                potentialDuplicates.Add(item);
            }
        }

        viewModel.PotentialDuplicates = potentialDuplicates
            .OrderByDescending(p => p.SimilarityScore)
            .ToList();

        return viewModel;
    }

    public async Task<bool> LinkComplaintsAsync(int sourceComplaintId, int targetComplaintId, 
        string linkType, string? notes, int userId, string userType)
    {
        // Validate complaints exist
        var sourceExists = await _context.Complaints.AnyAsync(c => c.ComplaintId == sourceComplaintId);
        var targetExists = await _context.Complaints.AnyAsync(c => c.ComplaintId == targetComplaintId);

        if (!sourceExists || !targetExists)
            return false;

        // Prevent self-linking
        if (sourceComplaintId == targetComplaintId)
            return false;

        // Check if already linked
        var alreadyLinked = await AreComplaintsLinkedAsync(sourceComplaintId, targetComplaintId);
        if (alreadyLinked)
            return false;

        var link = new ComplaintLink
        {
            SourceComplaintId = sourceComplaintId,
            TargetComplaintId = targetComplaintId,
            LinkType = linkType,
            Notes = notes,
            CreatedAt = DateTime.Now,
            CreatedByUserId = userId,
            CreatedByUserType = userType
        };

        _context.ComplaintLinks.Add(link);
        await _context.SaveChangesAsync();

        // Send notifications about the link
        var sourceComplaint = await _context.Complaints.FindAsync(sourceComplaintId);
        var targetComplaint = await _context.Complaints.FindAsync(targetComplaintId);

        if (sourceComplaint != null && targetComplaint != null)
        {
            await _notificationService.SendNotificationAsync(
                $"Your complaint has been linked to another complaint (#{targetComplaintId}) as {linkType}",
                sourceComplaintId,
                sourceComplaint.CitizenId,
                null,
                null
            );
        }

        return true;
    }

    public async Task<bool> UnlinkComplaintsAsync(int linkId)
    {
        var link = await _context.ComplaintLinks.FindAsync(linkId);
        if (link == null)
            return false;

        _context.ComplaintLinks.Remove(link);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<LinkedComplaintViewModel>> GetLinkedComplaintsAsync(int complaintId)
    {
        var outgoingLinks = await _context.ComplaintLinks
            .Include(l => l.TargetComplaint)
                .ThenInclude(c => c.Category)
            .Where(l => l.SourceComplaintId == complaintId)
            .Select(l => new LinkedComplaintViewModel
            {
                LinkId = l.LinkId,
                ComplaintId = l.TargetComplaintId,
                Title = l.TargetComplaint.Title,
                Description = l.TargetComplaint.Description,
                Status = l.TargetComplaint.Status,
                Priority = l.TargetComplaint.Priority,
                CategoryName = l.TargetComplaint.Category!.CategoryName,
                SubmittedAt = l.TargetComplaint.SubmittedAt,
                LinkType = l.LinkType,
                SimilarityScore = l.SimilarityScore,
                Notes = l.Notes,
                LinkedAt = l.CreatedAt,
                LinkedByUserType = l.CreatedByUserType
            })
            .ToListAsync();

        var incomingLinks = await _context.ComplaintLinks
            .Include(l => l.SourceComplaint)
                .ThenInclude(c => c.Category)
            .Where(l => l.TargetComplaintId == complaintId)
            .Select(l => new LinkedComplaintViewModel
            {
                LinkId = l.LinkId,
                ComplaintId = l.SourceComplaintId,
                Title = l.SourceComplaint.Title,
                Description = l.SourceComplaint.Description,
                Status = l.SourceComplaint.Status,
                Priority = l.SourceComplaint.Priority,
                CategoryName = l.SourceComplaint.Category!.CategoryName,
                SubmittedAt = l.SourceComplaint.SubmittedAt,
                LinkType = l.LinkType,
                SimilarityScore = l.SimilarityScore,
                Notes = l.Notes,
                LinkedAt = l.CreatedAt,
                LinkedByUserType = l.CreatedByUserType
            })
            .ToListAsync();

        return outgoingLinks.Concat(incomingLinks)
            .OrderByDescending(l => l.LinkedAt)
            .ToList();
    }

    public async Task<bool> MarkAsDuplicateAsync(int originalComplaintId, int duplicateComplaintId, 
        int userId, string userType)
    {
        var duplicate = await _context.Complaints.FindAsync(duplicateComplaintId);
        if (duplicate == null)
            return false;

        // Calculate similarity score
        var original = await _context.Complaints.FindAsync(originalComplaintId);
        if (original == null)
            return false;

        var similarityScore = CalculateSimilarityScore(original, duplicate);

        // Create link
        var link = new ComplaintLink
        {
            SourceComplaintId = originalComplaintId,
            TargetComplaintId = duplicateComplaintId,
            LinkType = "Duplicate",
            SimilarityScore = Math.Round(similarityScore, 2),
            Notes = "Marked as duplicate and auto-closed",
            CreatedAt = DateTime.Now,
            CreatedByUserId = userId,
            CreatedByUserType = userType
        };

        _context.ComplaintLinks.Add(link);

        // Update duplicate complaint status
        duplicate.Status = "Closed - Duplicate";
        duplicate.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        // Notify citizen
        await _notificationService.SendNotificationAsync(
            $"Your complaint has been marked as a duplicate of complaint #{originalComplaintId} and has been closed. " +
            $"Please refer to the original complaint for updates.",
            duplicateComplaintId,
            duplicate.CitizenId,
            null,
            null
        );

        return true;
    }

    public async Task<bool> AreComplaintsLinkedAsync(int complaintId1, int complaintId2)
    {
        return await _context.ComplaintLinks.AnyAsync(l =>
            (l.SourceComplaintId == complaintId1 && l.TargetComplaintId == complaintId2) ||
            (l.SourceComplaintId == complaintId2 && l.TargetComplaintId == complaintId1));
    }

    // ============================================================================
    // PRIVATE HELPER METHODS
    // ============================================================================

    private double CalculateSimilarityScore(Complaint complaint1, Complaint complaint2)
    {
        double titleScore = CalculateTextSimilarity(complaint1.Title, complaint2.Title);
        double descriptionScore = CalculateTextSimilarity(complaint1.Description, complaint2.Description);
        double locationScore = CalculateLocationSimilarity(complaint1, complaint2);
        double timeScore = CalculateTimeProximity(complaint1.SubmittedAt, complaint2.SubmittedAt);

        // Weighted average
        return (titleScore * 0.4) + (descriptionScore * 0.3) + (locationScore * 0.2) + (timeScore * 0.1);
    }

    private double CalculateTextSimilarity(string text1, string text2)
    {
        if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
            return 0;

        text1 = text1.ToLower().Trim();
        text2 = text2.ToLower().Trim();

        if (text1 == text2)
            return 100;

        // Levenshtein distance
        int distance = LevenshteinDistance(text1, text2);
        int maxLength = Math.Max(text1.Length, text2.Length);
        
        double similarity = (1.0 - ((double)distance / maxLength)) * 100;
        return Math.Max(0, similarity);
    }

    private int LevenshteinDistance(string s1, string s2)
    {
        int[,] d = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
            d[i, 0] = i;
        for (int j = 0; j <= s2.Length; j++)
            d[0, j] = j;

        for (int j = 1; j <= s2.Length; j++)
        {
            for (int i = 1; i <= s1.Length; i++)
            {
                int cost = (s1[i - 1] == s2[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[s1.Length, s2.Length];
    }

    private double CalculateLocationSimilarity(Complaint complaint1, Complaint complaint2)
    {
        // Text-based location comparison
        if (!string.IsNullOrWhiteSpace(complaint1.Location) && 
            !string.IsNullOrWhiteSpace(complaint2.Location))
        {
            var locationTextScore = CalculateTextSimilarity(complaint1.Location, complaint2.Location);
            
            // GPS-based proximity if available
            if (complaint1.Latitude.HasValue && complaint1.Longitude.HasValue &&
                complaint2.Latitude.HasValue && complaint2.Longitude.HasValue)
            {
                double distance = CalculateDistance(
                    complaint1.Latitude.Value, complaint1.Longitude.Value,
                    complaint2.Latitude.Value, complaint2.Longitude.Value);

                // Convert distance to similarity score (within 1km = high similarity)
                double gpsScore = Math.Max(0, 100 - (distance * 100));
                
                // Average of text and GPS scores
                return (locationTextScore + gpsScore) / 2;
            }
            
            return locationTextScore;
        }

        return 0;
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        // Haversine formula for calculating distance between GPS coordinates
        const double R = 6371; // Earth's radius in kilometers

        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    private double CalculateTimeProximity(DateTime date1, DateTime date2)
    {
        var diff = Math.Abs((date1 - date2).TotalDays);
        
        // Within same day = 100%, decreases over time
        if (diff < 1)
            return 100;
        if (diff < 3)
            return 75;
        if (diff < 7)
            return 50;
        if (diff < 14)
            return 25;
        
        return 0;
    }

    private string BuildSimilarityReason(Complaint complaint1, Complaint complaint2, double score)
    {
        var reasons = new List<string>();

        var titleSim = CalculateTextSimilarity(complaint1.Title, complaint2.Title);
        if (titleSim > 70)
            reasons.Add($"Similar titles ({titleSim:F0}%)");

        var descSim = CalculateTextSimilarity(complaint1.Description, complaint2.Description);
        if (descSim > 60)
            reasons.Add($"Similar descriptions ({descSim:F0}%)");

        if (!string.IsNullOrWhiteSpace(complaint1.Location) && 
            !string.IsNullOrWhiteSpace(complaint2.Location))
        {
            var locSim = CalculateTextSimilarity(complaint1.Location, complaint2.Location);
            if (locSim > 70)
                reasons.Add($"Same location ({locSim:F0}%)");
        }

        var timeDiff = Math.Abs((complaint1.SubmittedAt - complaint2.SubmittedAt).TotalDays);
        if (timeDiff < 1)
            reasons.Add("Submitted same day");
        else if (timeDiff < 3)
            reasons.Add($"Submitted {timeDiff:F0} days apart");

        return reasons.Any() ? string.Join(", ", reasons) : "Multiple similarities detected";
    }
}
