using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;

namespace ComplaintManagementSystem.Controllers;

public class HelpController : Controller
{
    private static readonly ConcurrentDictionary<string, (int helpful, int notHelpful)> _ratings = new();

    public IActionResult Index()
    {
        var faq = LoadFaq();
        return View(faq);
    }

    public IActionResult Faq(string? q, string? category)
    {
        var faq = LoadFaq();
        if (!string.IsNullOrWhiteSpace(category))
        {
            faq.Categories = faq.Categories
                .Where(c => string.Equals(c.Id, category, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
        if (!string.IsNullOrWhiteSpace(q))
        {
            var query = q.Trim();
            foreach (var cat in faq.Categories)
            {
                cat.Items = cat.Items
                    .Where(i => i.Question.Contains(query, StringComparison.OrdinalIgnoreCase) || i.Answer.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            faq.Categories = faq.Categories.Where(c => c.Items.Count > 0).ToList();
        }
        return View(faq);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Terms()
    {
        return View();
    }

    [HttpPost]
    public IActionResult RateAnswer([FromBody] RateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ItemId))
        {
            return BadRequest(new { ok = false });
        }

        _ratings.AddOrUpdate(request.ItemId,
            key => request.Helpful ? (1, 0) : (0, 1),
            (key, existing) => request.Helpful ? (existing.helpful + 1, existing.notHelpful) : (existing.helpful, existing.notHelpful + 1));

        var tuple = _ratings[request.ItemId];
        return Json(new { ok = true, helpful = tuple.helpful, notHelpful = tuple.notHelpful });
    }

    private FaqData LoadFaq()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "data", "faq.json");
        if (!System.IO.File.Exists(path))
        {
            return new FaqData { Categories = new List<FaqCategory>() };
        }
        var json = System.IO.File.ReadAllText(path);
        var data = JsonSerializer.Deserialize<FaqData>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new FaqData { Categories = new List<FaqCategory>() };

        foreach (var cat in data.Categories)
        {
            foreach (var item in cat.Items)
            {
                if (_ratings.TryGetValue(item.Id, out var r))
                {
                    item.Helpful = r.helpful;
                    item.NotHelpful = r.notHelpful;
                }
            }
        }

        return data;
    }

    public class FaqData
    {
        public List<FaqCategory> Categories { get; set; } = new();
    }

    public class FaqCategory
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<FaqItem> Items { get; set; } = new();
    }

    public class FaqItem
    {
        public string Id { get; set; } = string.Empty;
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        [JsonIgnore]
        public int Helpful { get; set; }
        [JsonIgnore]
        public int NotHelpful { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class RateRequest
    {
        public string ItemId { get; set; } = string.Empty;
        public bool Helpful { get; set; }
    }
}

