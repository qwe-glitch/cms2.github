global using ComplaintManagementSystem;
global using ComplaintManagementSystem.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using ComplaintManagementSystem.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

// Register custom services
builder.Services.AddScoped<ComplaintManagementSystem.Services.IComplaintService, ComplaintManagementSystem.Services.ComplaintService>();
builder.Services.AddScoped<ComplaintManagementSystem.Services.INotificationService, ComplaintManagementSystem.Services.NotificationService>();
builder.Services.AddScoped<ComplaintManagementSystem.Services.IFeedbackService, ComplaintManagementSystem.Services.FeedbackService>();
builder.Services.AddScoped<ComplaintManagementSystem.Services.IAdminService, ComplaintManagementSystem.Services.AdminService>();
builder.Services.AddScoped<ComplaintManagementSystem.Services.IEmailService, ComplaintManagementSystem.Services.EmailService>();
builder.Services.AddScoped<ComplaintManagementSystem.Services.ITriageService, ComplaintManagementSystem.Services.TriageService>();
builder.Services.AddScoped<ComplaintManagementSystem.Services.ICaseManagementService, ComplaintManagementSystem.Services.CaseManagementService>();
builder.Services.AddScoped<ComplaintManagementSystem.Services.IReportingService, ComplaintManagementSystem.Services.ReportingService>();
builder.Services.AddScoped<ComplaintManagementSystem.Services.IKnowledgeBaseService, ComplaintManagementSystem.Services.KnowledgeBaseService>();
builder.Services.AddScoped<ComplaintManagementSystem.Services.ChatService>();
builder.Services.AddSingleton<ComplaintManagementSystem.Services.ISpamDetectionService, ComplaintManagementSystem.Services.SpamDetectionService>();
builder.Services.AddHttpClient<ComplaintManagementSystem.Services.AiChatService>((serviceProvider, client) =>
{
    // Configuration is handled per-request in the service
});

builder.Services.AddSingleton<ComplaintManagementSystem.Services.ChatStorageService>();
builder.Services.AddScoped<ComplaintManagementSystem.Services.SafeDataService>();
builder.Services.AddScoped<ComplaintManagementSystem.Services.AiChatService>();
builder.Services.AddHostedService<ComplaintManagementSystem.Services.ComplaintReminderService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options => 
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "missing-client-id";
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "missing-client-secret";
    options.CallbackPath = "/signin-google";
});

builder.Services.AddAuthorization();
builder.Services.AddSignalR();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                     ?? "Data Source=complaints.db";

builder.Services.AddDbContext<DB>(options =>
{
    options.UseSqlite(connectionString);
});


var app = builder.Build();

// Seed database with default data
await SeedDatabaseAsync(app);

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRequestLocalization("en-MY");
app.UseSession();

// Add Authentication and Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Intro}/{action=Index}/{id?}");

app.MapHub<PrivateChatHub>("/privatechathub");
app.Run();

// Database seeding method
async Task SeedDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<DB>();
    
    // Ensure database is created
    await context.Database.EnsureCreatedAsync();
    
    // Check if database has data
    if (!context.Admins.Any())
    {
        Console.WriteLine("🌱 Seeding database with default data...");
        
        // Create default admin
        var admin = new Admin
        {
            Name = "System Administrator",
            Email = "admin@cms.com",
            PasswordHash = HashPassword("Admin123!"),
            CreatedAt = DateTime.Now,
            IsActive = true
        };
        context.Admins.Add(admin);
        
        // Create default departments
        var departments = new[]
        {
            new Department
            {
                DepartmentName = "Public Works",
                Location = "City Hall, Floor 2",
                Description = "Handles infrastructure and public facilities",
                OfficePhone = "03-1234-5678",
                OfficeEmail = "publicworks@city.gov",
                CreatedAt = DateTime.Now
            },
            new Department
            {
                DepartmentName = "Environmental Services",
                Location = "City Hall, Floor 3",
                Description = "Manages waste, sanitation, and environmental issues",
                OfficePhone = "03-1234-5679",
                OfficeEmail = "environment@city.gov",
                CreatedAt = DateTime.Now
            },
            new Department
            {
                DepartmentName = "Public Safety",
                Location = "City Hall, Floor 1",
                Description = "Handles security and safety concerns",
                OfficePhone = "03-1234-5680",
                OfficeEmail = "safety@city.gov",
                CreatedAt = DateTime.Now
            }
        };
        context.Departments.AddRange(departments);
        await context.SaveChangesAsync();
        
        // Create sample staff
        var staff = new Staff
        {
            Name = "John Doe",
            Email = "staff@cms.com",
            PasswordHash = HashPassword("Staff123!"),
            DepartmentId = departments[0].DepartmentId,
            Phone = "012-3456789",
            CreatedAt = DateTime.Now,
            IsActive = true
        };
        context.Staff.Add(staff);
        
        // Create default categories with triage configuration
        var categories = new[]
        {
            new Category 
            { 
                CategoryName = "Road & Infrastructure", 
                Description = "Potholes, damaged roads, broken streetlights",
                RiskLevel = "Medium",
                DefaultDepartmentId = departments[0].DepartmentId // Public Works
            },
            new Category 
            { 
                CategoryName = "Waste Management", 
                Description = "Garbage collection, illegal dumping",
                RiskLevel = "Low",
                DefaultDepartmentId = departments[1].DepartmentId // Environmental Services
            },
            new Category 
            { 
                CategoryName = "Water & Drainage", 
                Description = "Water supply issues, clogged drains, flooding",
                RiskLevel = "High",
                DefaultDepartmentId = departments[0].DepartmentId // Public Works
            },
            new Category 
            { 
                CategoryName = "Public Facilities", 
                Description = "Parks, playgrounds, public toilets",
                RiskLevel = "Low",
                DefaultDepartmentId = departments[0].DepartmentId // Public Works
            },
            new Category 
            { 
                CategoryName = "Noise Pollution", 
                Description = "Excessive noise from construction, events",
                RiskLevel = "Low",
                DefaultDepartmentId = departments[2].DepartmentId // Public Safety
            },
            new Category 
            { 
                CategoryName = "Illegal Parking", 
                Description = "Vehicles blocking roads or public spaces",
                RiskLevel = "Medium",
                DefaultDepartmentId = departments[2].DepartmentId // Public Safety
            },
            new Category 
            { 
                CategoryName = "Stray Animals", 
                Description = "Stray dogs, cats causing nuisance",
                RiskLevel = "Medium",
                DefaultDepartmentId = departments[1].DepartmentId // Environmental Services
            },
            new Category 
            { 
                CategoryName = "Others", 
                Description = "Other complaints not listed",
                RiskLevel = "Medium"
                // No default department - will require manual assignment
            }
        };
        context.Categories.AddRange(categories);
        
        await context.SaveChangesAsync();
        
        Console.WriteLine("✅ Database seeded successfully!");
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine("📧 DEFAULT ACCOUNTS:");
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.WriteLine("🔑 ADMIN LOGIN:");
        Console.WriteLine("   Email:    admin@cms.com");
        Console.WriteLine("   Password: Admin123!");
        Console.WriteLine();
        Console.WriteLine("👤 STAFF LOGIN:");
        Console.WriteLine("   Email:    staff@cms.com");
        Console.WriteLine("   Password: Staff123!");
        Console.WriteLine();
        Console.WriteLine("💡 CITIZEN:");
        Console.WriteLine("   Register a new account at /Account/Register");
        Console.WriteLine();
        Console.WriteLine("═══════════════════════════════════════════════════════");
        Console.WriteLine();
    }
}

string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
    return Convert.ToBase64String(hashedBytes);
}
