using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComplaintManagementSystem.Controllers
{
    public class PrivateChatController : Controller
    {
        private readonly DB _context;
        private readonly IWebHostEnvironment _environment;

        public PrivateChatController(DB context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            var userType = HttpContext.Session.GetString("UserType");
            if (userType != "Staff" && userType != "Admin")
            {
                return Unauthorized();
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
            {
                return BadRequest(new { error = "File size must be less than 5MB" });
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return BadRequest(new { error = "Invalid file type. Allowed: jpg, jpeg, png, gif, webp" });
            }

            // Create uploads directory if it doesn't exist
            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "chat");
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsPath, fileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return the URL path
            var url = $"/uploads/chat/{fileName}";
            return Ok(new { url });
        }

        public IActionResult Index()
        {
            var userType = HttpContext.Session.GetString("UserType");
            
            // Only Staff and Admin can access private chat
            if (userType != "Staff" && userType != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var userIdInt = HttpContext.Session.GetInt32("UserId");
            var currentUserId = userIdInt?.ToString() ?? string.Empty;
            var currentUserName = HttpContext.Session.GetString("UserName");

            // Get current user's department if they are staff
            int currentDepartmentId = 0;
            int currentStaffId = 0;
            if (userType == "Staff" && userIdInt.HasValue)
            {
                currentStaffId = userIdInt.Value;
                var staffMember = _context.Staff.FirstOrDefault(s => s.StaffId == currentStaffId);
                if (staffMember != null)
                {
                    currentDepartmentId = staffMember.DepartmentId;
                }
            }

            var availableUsers = new List<UserViewModel>();

            if (userType == "Admin")
            {
                // Admin can chat with ALL staff only (not other admins)
                var staffUsers = _context.Staff
                    .Where(s => s.IsActive)
                    .Select(s => new UserViewModel
                    {
                        UserId = s.StaffId.ToString(),
                        UserType = "Staff",
                        UserName = s.Name,
                        Department = s.Department.DepartmentName
                    })
                    .ToList();
                
                availableUsers = staffUsers;
            }
            else if (userType == "Staff")
            {
                // Staff can chat with ALL admins
                var adminUsers = _context.Admins
                    .Where(a => a.IsActive)
                    .Select(a => new UserViewModel
                    {
                        UserId = a.AdminId.ToString(),
                        UserType = "Admin",
                        UserName = a.Name,
                        Department = "Administration"
                    })
                    .ToList();
                
                // Staff can only chat with SAME DEPARTMENT staff
                var sameDeptStaff = _context.Staff
                    .Where(s => s.IsActive && 
                                s.StaffId != currentStaffId &&
                                s.DepartmentId == currentDepartmentId)
                    .Select(s => new UserViewModel
                    {
                        UserId = s.StaffId.ToString(),
                        UserType = "Staff",
                        UserName = s.Name,
                        Department = s.Department.DepartmentName
                    })
                    .ToList();
                
                availableUsers = adminUsers.Concat(sameDeptStaff).ToList();
            }

            // Sort: Admins first, then by name
            availableUsers = availableUsers
                .OrderBy(u => u.UserType == "Admin" ? 0 : 1)
                .ThenBy(u => u.UserName)
                .ToList();

            ViewBag.CurrentUserId = currentUserId;
            ViewBag.CurrentUserType = userType;
            ViewBag.CurrentUserName = currentUserName;

            return View(availableUsers);
        }
    }

    public class UserViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
    }
}
