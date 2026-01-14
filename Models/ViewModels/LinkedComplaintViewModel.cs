using System.ComponentModel.DataAnnotations;

namespace ComplaintManagementSystem.Models.ViewModels;

public class LinkedComplaintViewModel
{
    public int LinkId { get; set; }
    public int ComplaintId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    
    public string LinkType { get; set; } = string.Empty;
    public double? SimilarityScore { get; set; }
    public string? Notes { get; set; }
    public DateTime LinkedAt { get; set; }
    public string LinkedByUserType { get; set; } = string.Empty;
}

public class DuplicateDetectionViewModel
{
    public int OriginalComplaintId { get; set; }
    public string OriginalTitle { get; set; } = string.Empty;
    public string OriginalDescription { get; set; } = string.Empty;
    public string OriginalLocation { get; set; } = string.Empty;
    public DateTime OriginalSubmittedAt { get; set; }
    
    public List<PotentialDuplicateItem> PotentialDuplicates { get; set; } = new();
}

public class PotentialDuplicateItem
{
    public int ComplaintId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    
    public double SimilarityScore { get; set; }
    public string SimilarityReason { get; set; } = string.Empty;
    public int? AiSimilarityScore { get; set; }
    public string? AiReasoning { get; set; }
    public bool IsAlreadyLinked { get; set; }
}

public class LinkComplaintViewModel
{
    [Required]
    public int SourceComplaintId { get; set; }
    
    [Required]
    public int TargetComplaintId { get; set; }
    
    [Required, StringLength(20)]
    public string LinkType { get; set; } = "Related";
    
    [StringLength(500)]
    public string? Notes { get; set; }
}
