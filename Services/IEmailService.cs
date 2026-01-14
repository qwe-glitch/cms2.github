namespace ComplaintManagementSystem.Services;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
    Task SendVerificationEmailAsync(string toEmail, string name, string otpCode);
    Task SendPasswordResetEmailAsync(string toEmail, string name, string otpCode);
}
