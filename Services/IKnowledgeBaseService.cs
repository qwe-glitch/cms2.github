namespace ComplaintManagementSystem.Services;

/// <summary>
/// Interface for Knowledge Base search operations
/// </summary>
public interface IKnowledgeBaseService
{
    /// <summary>
    /// Search for resolved complaints that match the query text
    /// </summary>
    /// <param name="query">Search query (complaint title)</param>
    /// <param name="categoryId">Optional category filter</param>
    /// <param name="maxResults">Maximum number of results to return</param>
    /// <returns>List of knowledge base suggestions</returns>
    Task<List<KnowledgeBaseSuggestion>> SearchSuggestionsAsync(string query, int? categoryId = null, int maxResults = 5);
}

/// <summary>
/// Represents a knowledge base suggestion from a resolved complaint
/// </summary>
public class KnowledgeBaseSuggestion
{
    public int ComplaintId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ResolutionSummary { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime ResolvedAt { get; set; }
    public double RelevanceScore { get; set; }
}
