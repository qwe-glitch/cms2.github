using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ComplaintManagementSystem.Services;

/// <summary>
/// Implementation of spam detection using content analysis and rate limiting
/// </summary>
public class SpamDetectionService : ISpamDetectionService
{
    // In-memory storage for rate limiting (citizen ID -> list of submission times)
    private static readonly ConcurrentDictionary<int, List<DateTime>> _submissionHistory = new();
    
    // Configuration
    private const int MaxSubmissionsPerDay = 5;
    private const int MinTitleLength = 5;
    private const int MinDescriptionLength = 20;
    private const double MaxCapsPercentage = 0.5; // 50%
    private const int SpamThreshold = 50; // Score above this is considered spam

    // Spam keywords/patterns
    private static readonly string[] SpamKeywords = new[]
    {
        "test", "testing", "asdf", "qwerty", "xxx", "aaa", "bbb",
        "lorem ipsum", "buy now", "click here", "free money"
    };

    // Sensitive words / profanity list
    private static readonly string[] SensitiveWords = new[]
    {
        // Profanity
        "fuck", "shit", "damn", "ass", "bitch", "bastard", "crap",
        "dick", "cock", "pussy", "porn", "xxx", "nude", "naked",
        // Violence
        "kill", "murder", "rape", "terrorist", "bomb", "attack",
        // Insults
        "idiot", "stupid", "dumb", "retard", "moron", "hate",
        // Other offensive
        "badword", "offensive", "abuse", "sex"
    };

    public bool CheckSensitiveContent(string content, out string detectedWord)
    {
        detectedWord = string.Empty;
        if (string.IsNullOrWhiteSpace(content)) return false;

        var lowerContent = content.ToLower();
        foreach (var word in SensitiveWords)
        {
            // Simple containment check. For production, regex word boundary might be better,
            // but for this project, checking containment or whole word is usually enough.
            // Using boundaries to avoid flagging "assassin" for containing "ass".
            
            // Regex for whole word match: \bword\b
            if (Regex.IsMatch(lowerContent, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase))
            {
                detectedWord = word;
                return true;
            }
        }
        return false;
    }

    public Task<SpamCheckResult> CheckForSpamAsync(string title, string description, int citizenId)
    {
        var result = new SpamCheckResult
        {
            IsSpam = false,
            SpamScore = 0,
            Flags = new List<string>()
        };

        // 1. Check rate limiting
        CheckRateLimit(citizenId, result);

        // 2. Check content length
        CheckContentLength(title, description, result);

        // 3. Check for excessive caps
        CheckExcessiveCaps(title, description, result);

        // 4. Check for repetitive characters
        CheckRepetitiveCharacters(title, description, result);

        // 5. Check for spam keywords
        CheckSpamKeywords(title, description, result);

        // 6. Check for gibberish/random text
        CheckGibberish(title, description, result);

        // Determine if spam based on score
        result.IsSpam = result.SpamScore >= SpamThreshold;
        
        if (result.IsSpam)
        {
            result.Reason = $"Your submission was flagged as potential spam. Issues detected: {string.Join(", ", result.Flags)}";
        }

        return Task.FromResult(result);
    }

    private void CheckRateLimit(int citizenId, SpamCheckResult result)
    {
        CleanupOldRecords();
        
        if (_submissionHistory.TryGetValue(citizenId, out var submissions))
        {
            var recentSubmissions = submissions.Count(s => s > DateTime.Now.AddHours(-24));
            
            if (recentSubmissions >= MaxSubmissionsPerDay)
            {
                result.SpamScore += 40;
                result.Flags.Add($"Too many submissions ({recentSubmissions} in 24 hours)");
            }
            else if (recentSubmissions >= 3)
            {
                result.SpamScore += 15;
                result.Flags.Add("High submission frequency");
            }
        }
    }

    private void CheckContentLength(string title, string description, SpamCheckResult result)
    {
        if (title.Length < MinTitleLength)
        {
            result.SpamScore += 20;
            result.Flags.Add("Title too short");
        }

        if (description.Length < MinDescriptionLength)
        {
            result.SpamScore += 25;
            result.Flags.Add("Description too short");
        }
    }

    private void CheckExcessiveCaps(string title, string description, SpamCheckResult result)
    {
        var combined = title + " " + description;
        var letterCount = combined.Count(char.IsLetter);
        
        if (letterCount > 0)
        {
            var upperCount = combined.Count(char.IsUpper);
            var capsPercentage = (double)upperCount / letterCount;

            if (capsPercentage > MaxCapsPercentage && letterCount > 10)
            {
                result.SpamScore += 25;
                result.Flags.Add("Excessive use of capital letters");
            }
        }
    }

    private void CheckRepetitiveCharacters(string title, string description, SpamCheckResult result)
    {
        var combined = title + " " + description;
        
        // Check for 4+ repeated characters (e.g., "aaaa", "!!!!")
        var repetitivePattern = new Regex(@"(.)\1{3,}");
        var matches = repetitivePattern.Matches(combined);

        if (matches.Count > 0)
        {
            result.SpamScore += 20 * Math.Min(matches.Count, 3);
            result.Flags.Add("Repetitive characters detected");
        }

        // Check for repeated words (e.g., "test test test test")
        var words = combined.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length > 3)
        {
            var wordGroups = words.GroupBy(w => w).Where(g => g.Count() > 3);
            if (wordGroups.Any())
            {
                result.SpamScore += 20;
                result.Flags.Add("Repeated words detected");
            }
        }
    }

    private void CheckSpamKeywords(string title, string description, SpamCheckResult result)
    {
        var combined = (title + " " + description).ToLower();

        foreach (var keyword in SpamKeywords)
        {
            if (combined.Contains(keyword))
            {
                // Check if the entire content is just the spam keyword
                var trimmed = combined.Trim();
                if (trimmed == keyword || trimmed.StartsWith(keyword + " ") && trimmed.Length < keyword.Length + 10)
                {
                    result.SpamScore += 30;
                    result.Flags.Add($"Spam keyword detected: '{keyword}'");
                    break;
                }
            }
        }
    }

    private void CheckGibberish(string title, string description, SpamCheckResult result)
    {
        var combined = title + " " + description;
        
        // Check for random consonant patterns (no vowels in long words)
        var words = combined.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var gibberishWords = 0;

        foreach (var word in words.Where(w => w.Length > 4))
        {
            var hasVowel = word.Any(c => "aeiouAEIOU".Contains(c));
            if (!hasVowel)
            {
                gibberishWords++;
            }
        }

        if (gibberishWords > 2)
        {
            result.SpamScore += 25;
            result.Flags.Add("Possible gibberish text detected");
        }

        // NEW: Check for low vowel ratio (keyboard mashing like "eswdrtfhyuji")
        CheckLowVowelRatio(combined, result);
        
        // NEW: Check for keyboard pattern sequences
        CheckKeyboardPatterns(combined, result);

        // Check for excessive punctuation
        var punctuationCount = combined.Count(c => "!?.,;:".Contains(c));
        if (punctuationCount > combined.Length * 0.15 && combined.Length > 20)
        {
            result.SpamScore += 15;
            result.Flags.Add("Excessive punctuation");
        }
    }

    private void CheckLowVowelRatio(string text, SpamCheckResult result)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var word in words.Where(w => w.Length > 6))
        {
            var letters = word.Where(char.IsLetter).ToList();
            if (letters.Count < 6) continue;
            
            var vowels = letters.Count(c => "aeiouAEIOU".Contains(c));
            var vowelRatio = (double)vowels / letters.Count;
            
            // Normal English words have ~40% vowels
            // "eswdrtfhyuji" has 25% vowels (3/12) - suspicious!
            if (vowelRatio < 0.25)
            {
                result.SpamScore += 30;
                result.Flags.Add($"Random keyboard input detected");
                return; // Only flag once
            }
        }
    }

    private void CheckKeyboardPatterns(string text, SpamCheckResult result)
    {
        var lowerText = text.ToLower();
        
        // Common keyboard row patterns
        var keyboardPatterns = new[]
        {
            "qwer", "wert", "erty", "rtyu", "tyui", "yuio", "uiop",  // Top row
            "asdf", "sdfg", "dfgh", "fghj", "ghjk", "hjkl",          // Middle row
            "zxcv", "xcvb", "cvbn", "vbnm",                          // Bottom row
            "qazw", "wsxe", "edcr", "rfvt", "tgby", "yhnu", "ujmi",  // Diagonal patterns
            "1234", "2345", "3456", "4567", "5678", "6789", "7890"   // Number row
        };

        var patternCount = 0;
        foreach (var pattern in keyboardPatterns)
        {
            if (lowerText.Contains(pattern))
            {
                patternCount++;
            }
        }

        if (patternCount >= 2)
        {
            result.SpamScore += 25;
            result.Flags.Add("Keyboard pattern detected");
        }
    }

    public void RecordSubmissionAttempt(int citizenId)
    {
        _submissionHistory.AddOrUpdate(
            citizenId,
            new List<DateTime> { DateTime.Now },
            (key, existing) =>
            {
                existing.Add(DateTime.Now);
                return existing;
            }
        );
    }

    public void CleanupOldRecords()
    {
        var cutoff = DateTime.Now.AddHours(-24);
        
        foreach (var kvp in _submissionHistory)
        {
            kvp.Value.RemoveAll(dt => dt < cutoff);
            
            // Remove empty entries
            if (kvp.Value.Count == 0)
            {
                _submissionHistory.TryRemove(kvp.Key, out _);
            }
        }
    }
}
