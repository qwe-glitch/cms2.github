namespace ComplaintManagementSystem.Services;

/// <summary>
/// Result of spam detection check
/// </summary>
public class SpamCheckResult
{
    public bool IsSpam { get; set; }
    public int SpamScore { get; set; } // 0-100
    public string Reason { get; set; } = string.Empty;
    public List<string> Flags { get; set; } = new();
}

/// <summary>
/// Interface for spam detection operations
/// </summary>
public interface ISpamDetectionService
{
    /// <summary>
    /// Check if a complaint submission appears to be spam
    /// </summary>
    /// <param name="title">Complaint title</param>
    /// <param name="description">Complaint description</param>
    /// <param name="citizenId">ID of the citizen submitting</param>
    /// <returns>SpamCheckResult with spam status and details</returns>
    Task<SpamCheckResult> CheckForSpamAsync(string title, string description, int citizenId);
    
    /// <summary>
    /// Record a submission attempt for rate limiting
    /// </summary>
    void RecordSubmissionAttempt(int citizenId);
    
    /// <summary>
    /// Check if content contains sensitive words (profanity, etc.)
    /// </summary>
    /// <param name="content">The text content to check</param>
    /// <param name="detectedWord">The sensitive word found, if any</param>
    /// <returns>True if sensitive content is found</returns>
    bool CheckSensitiveContent(string content, out string detectedWord);

    /// <summary>
    /// Clear old submission records (cleanup)
    /// </summary>
    void CleanupOldRecords();
}
