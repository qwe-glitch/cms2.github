using ComplaintManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagementSystem.Services;

/// <summary>
/// Implementation of Knowledge Base search operations
/// Searches resolved complaints to suggest similar issues before submission
/// </summary>
public class KnowledgeBaseService : IKnowledgeBaseService
{
    private readonly DB _context;
    private const double RELEVANCE_THRESHOLD = 40.0; // Lower threshold for broader suggestions

    public KnowledgeBaseService(DB context)
    {
        _context = context;
    }

    public async Task<List<KnowledgeBaseSuggestion>> SearchSuggestionsAsync(string query, int? categoryId = null, int maxResults = 5)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
            return new List<KnowledgeBaseSuggestion>();

        // Get resolved/closed complaints
        var resolvedStatuses = new[] { "Resolved", "Closed", "Closed - Duplicate" };
        
        var candidatesQuery = _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.ComplaintHistories)
            .Where(c => resolvedStatuses.Contains(c.Status) && !c.IsAnonymous);

        // Filter by category if specified
        if (categoryId.HasValue)
        {
            candidatesQuery = candidatesQuery.Where(c => c.CategoryId == categoryId.Value);
        }

        var candidates = await candidatesQuery
            .OrderByDescending(c => c.ResolvedAt ?? c.UpdatedAt)
            .Take(100) // Limit candidates for performance
            .ToListAsync();

        var suggestions = new List<KnowledgeBaseSuggestion>();
        var queryLower = query.ToLower().Trim();
        var queryWords = queryLower.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var complaint in candidates)
        {
            var score = CalculateRelevanceScore(queryLower, queryWords, complaint);

            if (score >= RELEVANCE_THRESHOLD)
            {
                // Get the latest resolution note from history
                var resolutionNote = complaint.ComplaintHistories
                    .Where(h => h.StatusAfter != null && 
                           (h.StatusAfter.Contains("Resolved") || h.StatusAfter.Contains("Closed")))
                    .OrderByDescending(h => h.ChangeTime)
                    .Select(h => h.Notes)
                    .FirstOrDefault();

                suggestions.Add(new KnowledgeBaseSuggestion
                {
                    ComplaintId = complaint.ComplaintId,
                    Title = complaint.Title,
                    Description = TruncateDescription(complaint.Description, 150),
                    ResolutionSummary = TruncateDescription(resolutionNote, 100),
                    CategoryName = complaint.Category?.CategoryName ?? "Unknown",
                    ResolvedAt = complaint.ResolvedAt ?? complaint.UpdatedAt ?? complaint.SubmittedAt,
                    RelevanceScore = Math.Round(score, 2)
                });
            }
        }

        return suggestions
            .OrderByDescending(s => s.RelevanceScore)
            .Take(maxResults)
            .ToList();
    }

    private double CalculateRelevanceScore(string queryLower, string[] queryWords, Complaint complaint)
    {
        var titleLower = complaint.Title.ToLower();
        var descLower = complaint.Description.ToLower();

        double score = 0;

        // Exact phrase match in title (highest weight)
        if (titleLower.Contains(queryLower))
        {
            score += 50;
        }

        // Word matches in title
        foreach (var word in queryWords)
        {
            if (word.Length >= 3 && titleLower.Contains(word))
            {
                score += 15;
            }
        }

        // Exact phrase match in description
        if (descLower.Contains(queryLower))
        {
            score += 20;
        }

        // Word matches in description
        foreach (var word in queryWords)
        {
            if (word.Length >= 3 && descLower.Contains(word))
            {
                score += 5;
            }
        }

        // Text similarity (Levenshtein-based)
        var titleSimilarity = CalculateTextSimilarity(queryLower, titleLower);
        score += titleSimilarity * 0.3;

        // Cap at 100
        return Math.Min(score, 100);
    }

    private double CalculateTextSimilarity(string text1, string text2)
    {
        if (string.IsNullOrWhiteSpace(text1) || string.IsNullOrWhiteSpace(text2))
            return 0;

        // Use word overlap for longer texts
        var words1 = text1.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var words2 = text2.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        if (words1.Count == 0 || words2.Count == 0)
            return 0;

        var intersection = words1.Intersect(words2).Count();
        var union = words1.Union(words2).Count();

        // Jaccard similarity
        return (double)intersection / union * 100;
    }

    private string TruncateDescription(string? text, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength).TrimEnd() + "...";
    }
}
