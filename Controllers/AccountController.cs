using ComplaintManagementSystem.Models;
using ComplaintManagementSystem.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ComplaintManagementSystem.Controllers;

/// <summary>
/// Handles authentication and registration for all user types
/// </summary>
public class AccountController : Controller
{
    private readonly DB _context;
    private readonly ComplaintManagementSystem.Services.IEmailService _emailService;

    public AccountController(DB context, ComplaintManagementSystem.Services.IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    // GET: /Account/Login
    [HttpGet]
    public IActionResult Login()
    {
        var model = new LoginViewModel();
        if (Request.Cookies.TryGetValue("RememberMeEmail", out string? email) && !string.IsNullOrEmpty(email))
        {
            model.Email = email;
            model.RememberMe = true;
        }
        return View(model);
    }

    // POST: /Account/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var passwordHash = HashPassword(model.Password);

        // Auto-detect user type by checking all tables sequentially
        
        // 1. Check Citizens table first
        var citizen = await _context.Citizens
            .FirstOrDefaultAsync(c => c.Email == model.Email);

        if (citizen != null)
        {
            // Check if locked
            if (citizen.IsLocked)
            {
                ModelState.AddModelError("", "Your account has been locked due to multiple failed login attempts. Please contact an administrator.");
                return View(model);
            }

            // Check if email is verified
            if (!citizen.IsEmailVerified)
            {
                TempData["Email"] = citizen.Email;
                TempData["ErrorMessage"] = "Please verify your email before logging in.";
                return RedirectToAction(nameof(VerifyEmail));
            }

            // Check password and active status
            if (citizen.PasswordHash == passwordHash && citizen.IsActive)
            {
                // Reset failed attempts on successful login
                if (citizen.FailedLoginAttempts > 0)
                {
                    citizen.FailedLoginAttempts = 0;
                    await _context.SaveChangesAsync();
                }

                // Set session/cookie
                HttpContext.Session.SetInt32("UserId", citizen.CitizenId);
                HttpContext.Session.SetString("UserType", "Citizen");
                HttpContext.Session.SetString("UserName", citizen.Name);

                HandleRememberMe(model);

                return RedirectToAction("Index", "Complaint");
            }
            else if (!citizen.IsActive)
            {
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }
            else
            {
                // Incorrect password - increment failed attempts
                citizen.FailedLoginAttempts++;
                
                if (citizen.FailedLoginAttempts >= 3)
                {
                    citizen.IsLocked = true;
                    await _context.SaveChangesAsync();
                    ModelState.AddModelError("", "Your account has been locked due to 3 failed login attempts. Please contact an administrator.");
                    return View(model);
                }
                
                await _context.SaveChangesAsync();
                
                // Show warning if close to locking
                if (citizen.FailedLoginAttempts == 2)
                {
                    ModelState.AddModelError("", "Invalid password. Warning: One more failed attempt will lock your account.");
                    return View(model);
                }
                
                ModelState.AddModelError("", "Invalid email or password");
                return View(model);
            }
        }

        // 2. Check Staff table if not found in Citizens
        var staff = await _context.Staff
            .FirstOrDefaultAsync(s => s.Email == model.Email && s.PasswordHash == passwordHash && s.IsActive);

        if (staff != null)
        {
            HttpContext.Session.SetInt32("UserId", staff.StaffId);
            HttpContext.Session.SetString("UserType", "Staff");
            HttpContext.Session.SetString("UserName", staff.Name);

            HandleRememberMe(model);

            return RedirectToAction("Index", "Complaint");
        }

        // 3. Check Admins table if not found in Staff
        var admin = await _context.Admins
            .FirstOrDefaultAsync(a => a.Email == model.Email && a.PasswordHash == passwordHash && a.IsActive);

        if (admin != null)
        {
            HttpContext.Session.SetInt32("UserId", admin.AdminId);
            HttpContext.Session.SetString("UserType", "Admin");
            HttpContext.Session.SetString("UserName", admin.Name);

            HandleRememberMe(model);

            return RedirectToAction("Dashboard", "Admin");
        }

        // Not found in any table or credentials invalid
        ModelState.AddModelError("", "Invalid email or password");
        return View(model);
    }

    // GET: /Account/Register
    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    // POST: /Account/Register (Citizen only)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Check if email already exists
        var emailExists = await _context.Citizens.AnyAsync(c => c.Email == model.Email);
        if (emailExists)
        {
            ModelState.AddModelError("Email", "Email is already registered");
            return View(model);
        }

        // Generate OTP
        var otpCode = GenerateOTP();
        var tokenExpiry = DateTime.Now.AddMinutes(15);

        var citizen = new Citizen
        {
            Name = model.Name,
            Email = model.Email,
            PasswordHash = HashPassword(model.Password),
            Phone = model.Phone,
            Address = model.Address,
            CreatedAt = DateTime.Now,
            IsActive = true,
            IsEmailVerified = false,
            EmailVerificationToken = otpCode,
            TokenExpiresAt = tokenExpiry
        };

        _context.Citizens.Add(citizen);
        await _context.SaveChangesAsync();

        // Send OTP email
        await _emailService.SendVerificationEmailAsync(citizen.Email, citizen.Name, otpCode);

        TempData["Email"] = citizen.Email;
        TempData["SuccessMessage"] = "Registration successful! Please check your email for the verification code.";
        return RedirectToAction(nameof(VerifyEmail));
    }

    // GET: /Account/VerifyEmail
    [HttpGet]
    public IActionResult VerifyEmail()
    {
        var email = TempData["Email"]?.ToString();
        if (string.IsNullOrEmpty(email))
        {
            if (TempData["ErrorMessage"] != null)
            {
                // Allow showing error message even if email is lost from tempdata, 
                // user can manually enter email if we add an input field, 
                // but for now let's redirect to Login if no context
            }
            // If we are here from Login redirect, TempData["Email"] should be set.
            // If user refreshes, it might be lost.
            // Let's just return View, view should handle null email gracefully or ask user to login again
        }
        ViewBag.Email = email;
        return View();
    }

    // POST: /Account/VerifyEmail
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyEmail(string email, string otpCode)
    {
        var citizen = await _context.Citizens
            .FirstOrDefaultAsync(c => c.Email == email && c.EmailVerificationToken == otpCode);

        if (citizen == null)
        {
            ModelState.AddModelError("", "Invalid verification code or email");
            ViewBag.Email = email;
            return View();
        }

        if (citizen.TokenExpiresAt < DateTime.Now)
        {
            ModelState.AddModelError("", "Verification code has expired. Please request a new one.");
            ViewBag.Email = email;
            return View();
        }

        citizen.IsEmailVerified = true;
        citizen.EmailVerificationToken = null;
        citizen.TokenExpiresAt = null;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Email verified successfully! You can now login.";
        return RedirectToAction(nameof(Login));
    }

    // POST: /Account/ResendOTP
    [HttpPost]
    public async Task<IActionResult> ResendOTP(string email)
    {
        var citizen = await _context.Citizens.FirstOrDefaultAsync(c => c.Email == email);
        if (citizen == null || citizen.IsEmailVerified)
        {
            return Json(new { success = false, message = "Invalid request" });
        }

        var otpCode = GenerateOTP();
        citizen.EmailVerificationToken = otpCode;
        citizen.TokenExpiresAt = DateTime.Now.AddMinutes(15);
        await _context.SaveChangesAsync();

        await _emailService.SendVerificationEmailAsync(citizen.Email, citizen.Name, otpCode);

        return Json(new { success = true, message = "New verification code sent to your email" });
    }

    // SSO Actions
    [HttpGet]
    public IActionResult ExternalLogin(string provider)
    {
        var redirectUrl = Url.Action(nameof(ExternalLoginCallback));
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, provider);
    }

    [HttpGet]
    public async Task<IActionResult> ExternalLoginCallback()
    {
        var info = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (info?.Principal == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        var name = info.Principal.FindFirstValue(ClaimTypes.Name);
        var provider = info.Properties?.Items[".AuthScheme"];
        var externalId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(email))
        {
             TempData["ErrorMessage"] = "Email not received from external provider.";
             return RedirectToAction(nameof(Login));
        }

        var citizen = await _context.Citizens.FirstOrDefaultAsync(c => c.Email == email);
        
        if (citizen == null)
        {
            // New user - redirect to completion page to set password
            TempData["Ext_Email"] = email;
            TempData["Ext_Name"] = name ?? email.Split('@')[0];
            TempData["Ext_Provider"] = provider;
            TempData["Ext_Id"] = externalId;
            
            return RedirectToAction(nameof(CompleteExternalRegistration));
        }
        else
        {
            // Existing user
            // Link account if not linked
            if (string.IsNullOrEmpty(citizen.ExternalId))
            {
                citizen.ExternalProvider = provider;
                citizen.ExternalId = externalId;
                await _context.SaveChangesAsync();
            }

            // Check verification
            if (!citizen.IsEmailVerified)
            {
                TempData["Email"] = citizen.Email;
                TempData["ErrorMessage"] = "Please verify your email before logging in.";
                return RedirectToAction(nameof(VerifyEmail));
            }

            // Check if locked
            if (citizen.IsLocked)
            {
                TempData["ErrorMessage"] = "Your account is locked. Please contact support.";
                return RedirectToAction(nameof(Login));
            }

            // Login
            HttpContext.Session.SetInt32("UserId", citizen.CitizenId);
            HttpContext.Session.SetString("UserType", "Citizen");
            HttpContext.Session.SetString("UserName", citizen.Name);

            return RedirectToAction("Index", "Complaint");
        }
    }

    [HttpGet]
    public IActionResult CompleteExternalRegistration()
    {
        if (TempData["Ext_Email"] == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var model = new CompleteExternalRegistrationViewModel
        {
            Email = TempData["Ext_Email"]?.ToString() ?? "",
            Name = TempData["Ext_Name"]?.ToString() ?? "",
            Provider = TempData["Ext_Provider"]?.ToString() ?? "",
            ExternalId = TempData["Ext_Id"]?.ToString() ?? ""
        };

        // Keep data for post
        TempData.Keep("Ext_Email");
        TempData.Keep("Ext_Name");
        TempData.Keep("Ext_Provider");
        TempData.Keep("Ext_Id");

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteExternalRegistration(CompleteExternalRegistrationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Double check if email exists
        if (await _context.Citizens.AnyAsync(c => c.Email == model.Email))
        {
            ModelState.AddModelError("", "Account with this email already exists. Please login and link your account.");
            return View(model);
        }

        var otpCode = GenerateOTP();
        var tokenExpiry = DateTime.Now.AddMinutes(15);

        var citizen = new Citizen
        {
            Name = model.Name,
            Email = model.Email,
            PasswordHash = HashPassword(model.Password),
            CreatedAt = DateTime.Now,
            IsActive = true,
            IsEmailVerified = false, // Require verification
            EmailVerificationToken = otpCode,
            TokenExpiresAt = tokenExpiry,
            ExternalProvider = model.Provider,
            ExternalId = model.ExternalId
        };

        _context.Citizens.Add(citizen);
        await _context.SaveChangesAsync();

        // Send OTP
        await _emailService.SendVerificationEmailAsync(citizen.Email, citizen.Name, otpCode);

        TempData["Email"] = citizen.Email;
        TempData["SuccessMessage"] = "Registration successful! Please check your email for the verification code.";
        
        return RedirectToAction(nameof(VerifyEmail));
    }

    // GET: /Account/Logout
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    // GET: /Account/Profile
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null || userType != "Citizen")
        {
            return RedirectToAction(nameof(Login));
        }

        var citizen = await _context.Citizens.FindAsync(userId.Value);
        if (citizen == null)
        {
            return RedirectToAction(nameof(Login));
        }

        var model = new ProfileViewModel
        {
            Name = citizen.Name,
            Email = citizen.Email,
            Phone = citizen.Phone,
            Address = citizen.Address
        };

        return View(model);
    }

    // POST: /Account/Profile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        var userId = HttpContext.Session.GetInt32("UserId");
        var userType = HttpContext.Session.GetString("UserType");

        if (userId == null || userType != "Citizen")
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var citizen = await _context.Citizens.FindAsync(userId.Value);
        if (citizen == null)
        {
            return RedirectToAction(nameof(Login));
        }

        // Check if email is changed and already exists
        if (model.Email != citizen.Email)
        {
            var emailExists = await _context.Citizens
                .AnyAsync(c => c.Email == model.Email && c.CitizenId != userId.Value);
            
            if (emailExists)
            {
                ModelState.AddModelError("Email", "Email is already in use");
                return View(model);
            }
        }

        // Update fields
        citizen.Name = model.Name;
        citizen.Email = model.Email;
        citizen.Phone = model.Phone;
        citizen.Address = model.Address;

        // Update password if provided
        if (!string.IsNullOrWhiteSpace(model.NewPassword))
        {
            citizen.PasswordHash = HashPassword(model.NewPassword);
        }

        // Update session username if changed
        if (citizen.Name != HttpContext.Session.GetString("UserName"))
        {
            HttpContext.Session.SetString("UserName", citizen.Name);
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Profile updated successfully!";
        return RedirectToAction(nameof(Profile));
    }

    // GET: /Account/ForgotPassword
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    // POST: /Account/ForgotPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            ModelState.AddModelError("", "Please enter your email address.");
            return View();
        }

        var citizen = await _context.Citizens.FirstOrDefaultAsync(c => c.Email == email);
        if (citizen == null)
        {
            // Don't reveal that the user doesn't exist
            TempData["SuccessMessage"] = "If an account exists with that email, we have sent a password reset code.";
            return View();
        }

        // Generate OTP
        var otpCode = GenerateOTP();
        citizen.EmailVerificationToken = otpCode;
        citizen.TokenExpiresAt = DateTime.Now.AddMinutes(15);
        await _context.SaveChangesAsync();

        // Send Email
        await _emailService.SendPasswordResetEmailAsync(citizen.Email, citizen.Name, otpCode);

        TempData["ResetEmail"] = citizen.Email;
        return RedirectToAction(nameof(VerifyResetCode));
    }

    // GET: /Account/VerifyResetCode
    [HttpGet]
    public IActionResult VerifyResetCode()
    {
        var email = TempData["ResetEmail"]?.ToString();
        if (string.IsNullOrEmpty(email))
        {
            return RedirectToAction(nameof(ForgotPassword));
        }
        ViewBag.Email = email;
        TempData.Keep("ResetEmail");
        return View();
    }

    // POST: /Account/VerifyResetCode
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyResetCode(string email, string otpCode)
    {
        var citizen = await _context.Citizens
            .FirstOrDefaultAsync(c => c.Email == email && c.EmailVerificationToken == otpCode);

        if (citizen == null)
        {
            ModelState.AddModelError("", "Invalid code or email.");
            ViewBag.Email = email;
            TempData.Keep("ResetEmail");
            return View();
        }

        if (citizen.TokenExpiresAt < DateTime.Now)
        {
            ModelState.AddModelError("", "Code has expired. Please request a new one.");
            ViewBag.Email = email;
            TempData.Keep("ResetEmail");
            return View();
        }

        // Code is valid, allow reset
        // We can use a temp token or just pass the email to the next step securely
        // For simplicity, we'll use TempData but ideally we should use a signed token
        // To prevent skipping this step, we'll keep the token in DB but maybe mark it as verified?
        // Or just redirect to ResetPassword with the email in TempData and re-verify the token there?
        // Let's re-verify token in ResetPassword to be safe.
        
        TempData["ResetEmail"] = email;
        TempData["ResetToken"] = otpCode; // Pass the token to verify again
        
        return RedirectToAction(nameof(ResetPassword));
    }

    // GET: /Account/ResetPassword
    [HttpGet]
    public IActionResult ResetPassword()
    {
        var email = TempData["ResetEmail"]?.ToString();
        var token = TempData["ResetToken"]?.ToString();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        TempData.Keep("ResetEmail");
        TempData.Keep("ResetToken");
        
        return View();
    }

    // POST: /Account/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        var email = TempData["ResetEmail"]?.ToString();
        var token = TempData["ResetToken"]?.ToString();

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
        {
            return RedirectToAction(nameof(ForgotPassword));
        }

        TempData.Keep("ResetEmail");
        TempData.Keep("ResetToken");

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var citizen = await _context.Citizens
            .FirstOrDefaultAsync(c => c.Email == email && c.EmailVerificationToken == token);

        if (citizen == null || citizen.TokenExpiresAt < DateTime.Now)
        {
            ModelState.AddModelError("", "Invalid or expired session. Please start over.");
            return RedirectToAction(nameof(ForgotPassword));
        }

        // Update Password
        citizen.PasswordHash = HashPassword(model.NewPassword);
        citizen.EmailVerificationToken = null;
        citizen.TokenExpiresAt = null;
        
        // Also unlock account if it was locked
        citizen.IsLocked = false;
        citizen.FailedLoginAttempts = 0;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Password reset successfully! You can now login.";
        return RedirectToAction(nameof(Login));
    }

    #region Helper Methods

    private string HashPassword(string password)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    private string GenerateOTP()
    {
        return new Random().Next(100000, 999999).ToString();
    }

    private void HandleRememberMe(LoginViewModel model)
    {
        if (model.RememberMe)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(30),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            };
            Response.Cookies.Append("RememberMeEmail", model.Email, cookieOptions);
        }
        else
        {
            Response.Cookies.Delete("RememberMeEmail");
        }
    }

    #endregion
}
