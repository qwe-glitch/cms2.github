using Microsoft.AspNetCore.Mvc;
using ComplaintManagementSystem.Models.ViewModels;
using ComplaintManagementSystem.Services;

namespace ComplaintManagementSystem.Controllers;

public class ContactController : Controller
{
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public ContactController(IEmailService emailService, IConfiguration configuration)
    {
        _emailService = emailService;
        _configuration = configuration;
    }

    [HttpGet("/Contact")]
    public IActionResult Index()
    {
        return View(new ContactFormViewModel());
    }

    [HttpPost("/Contact")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ContactFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var toEmail = _configuration["Email:SupportEmail"] ?? "support@citizenvoice.gov";
        var subject = $"Contact: {model.Subject}";
        var body = $@"<div><p><strong>Name:</strong> {model.Name}</p><p><strong>Email:</strong> {model.Email}</p><p><strong>Subject:</strong> {model.Subject}</p><p><strong>Message:</strong></p><p>{System.Net.WebUtility.HtmlEncode(model.Message).Replace("\n", "<br>")}</p></div>";

        await _emailService.SendEmailAsync(toEmail, subject, body);

        TempData["SuccessMessage"] = "Your message has been sent.";
        return RedirectToAction(nameof(Index));
    }
}
