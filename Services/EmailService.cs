using System.Net;
using System.Net.Mail;

namespace ComplaintManagementSystem.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        var smtpHost = _configuration["Email:SmtpHost"];
        var smtpPortStr = _configuration["Email:SmtpPort"];
        
        // Default to port 587 if not specified
        int smtpPort = 587;
        if (!string.IsNullOrEmpty(smtpPortStr))
        {
            int.TryParse(smtpPortStr, out smtpPort);
        }

        var smtpUser = _configuration["Email:SmtpUser"];
        var smtpPass = _configuration["Email:SmtpPass"];
        var fromEmail = _configuration["Email:FromEmail"];
        var fromName = _configuration["Email:FromName"];

        // If email config is missing, log warning and return (for development without email setup)
        if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
        {
            Console.WriteLine($"[WARNING] Email configuration missing. Email to {toEmail} not sent.");
            Console.WriteLine($"Subject: {subject}");
            return;
        }

        using var client = new SmtpClient(smtpHost, smtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(smtpUser, smtpPass)
        };

        var message = new MailMessage
        {
            From = new MailAddress(fromEmail ?? "noreply@citizenvoice.gov", fromName ?? "Segamat Community"),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        try 
        {
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to send email: {ex.Message}");
            // In production, you might want to throw or log to a proper logger
        }
    }

    public async Task SendVerificationEmailAsync(string toEmail, string name, string otpCode)
    {
        var subject = "Verify Your Email - Segamat Community";
        var htmlBody = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background: linear-gradient(135deg, #0d6efd 0%, #0a58ca 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                    .content {{ background: #ffffff; padding: 30px; border: 1px solid #e9ecef; border-top: none; border-radius: 0 0 10px 10px; }}
                    .otp-box {{ background: #f8f9fa; border: 2px dashed #0d6efd; border-radius: 10px; padding: 20px; text-align: center; margin: 25px 0; }}
                    .otp-code {{ font-size: 36px; font-weight: bold; color: #0d6efd; letter-spacing: 8px; font-family: monospace; }}
                    .footer {{ text-align: center; padding: 20px; color: #6c757d; font-size: 12px; margin-top: 20px; }}
                    .logo-icon {{ font-size: 40px; margin-bottom: 10px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <div class='logo-icon'>🛡️</div>
                        <h1 style='margin:0;'>Email Verification</h1>
                    </div>
                    <div class='content'>
                        <p>Hello <strong>{name}</strong>,</p>
                        <p>Thank you for registering with <strong>Segamat Community</strong> Complaint Management System!</p>
                        <p>To complete your registration and verify your account, please use the following One-Time Password (OTP):</p>
                        
                        <div class='otp-box'>
                            <div class='otp-code'>{otpCode}</div>
                        </div>
                        
                        <p style='text-align: center; color: #dc3545;'><strong>This code will expire in 15 minutes.</strong></p>
                        
                        <p>If you did not create an account, please ignore this email.</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; {DateTime.Now.Year} Segamat Community Complaint Management System. All rights reserved.</p>
                        <p>This is an automated message, please do not reply.</p>
                    </div>
                </div>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, htmlBody);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string name, string otpCode)
    {
        var subject = "Reset Your Password - Segamat Community";
        var htmlBody = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background: linear-gradient(135deg, #dc3545 0%, #b02a37 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                    .content {{ background: #ffffff; padding: 30px; border: 1px solid #e9ecef; border-top: none; border-radius: 0 0 10px 10px; }}
                    .otp-box {{ background: #f8f9fa; border: 2px dashed #dc3545; border-radius: 10px; padding: 20px; text-align: center; margin: 25px 0; }}
                    .otp-code {{ font-size: 36px; font-weight: bold; color: #dc3545; letter-spacing: 8px; font-family: monospace; }}
                    .footer {{ text-align: center; padding: 20px; color: #6c757d; font-size: 12px; margin-top: 20px; }}
                    .logo-icon {{ font-size: 40px; margin-bottom: 10px; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <div class='logo-icon'>🔒</div>
                        <h1 style='margin:0;'>Password Reset</h1>
                    </div>
                    <div class='content'>
                        <p>Hello <strong>{name}</strong>,</p>
                        <p>We received a request to reset the password for your <strong>Segamat Community</strong> account.</p>
                        <p>Please use the following code to reset your password:</p>
                        
                        <div class='otp-box'>
                            <div class='otp-code'>{otpCode}</div>
                        </div>
                        
                        <p style='text-align: center; color: #dc3545;'><strong>This code will expire in 15 minutes.</strong></p>
                        
                        <p>If you did not request a password reset, please ignore this email or contact support if you have concerns.</p>
                    </div>
                    <div class='footer'>
                        <p>&copy; {DateTime.Now.Year} Segamat Community Complaint Management System. All rights reserved.</p>
                        <p>This is an automated message, please do not reply.</p>
                    </div>
                </div>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, htmlBody);
    }
}
