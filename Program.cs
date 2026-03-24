using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using SocialHelpDonation.Data;
using SocialHelpDonation.Services;

// Load .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ─── Read DB credentials from environment ────────────────────────────────────
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "donation_db";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";

var connectionString = $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPassword};AllowPublicKeyRetrieval=true;SslMode=None;";

// ─── Services ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<PasswordService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// ─── Auto-create DB and tables ────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.EnsureCreated();

        // Seed default admin if none exists
        var passwordService = scope.ServiceProvider.GetRequiredService<PasswordService>();
        if (!db.Admins.Any())
        {
            db.Admins.Add(new SocialHelpDonation.Models.Admin { Name = "Super Admin", Email = "admin@donationsystem.com", PasswordHash = passwordService.HashPassword("Admin@123") });
            db.SaveChanges();
        }

        // Seed Sample Organisations & Requirements
        if (!db.Organisations.Any())
        {
            var org1 = new SocialHelpDonation.Models.Organisation
            {
                Name = "Triya Orphanage", Email = "contact@triyaorphanage.org", PasswordHash = passwordService.HashPassword("Org@123"), Phone = "9876543210", Address = "123 Hope Street, City", OrgType = SocialHelpDonation.Models.OrgType.Orphanage, Status = SocialHelpDonation.Models.OrgStatus.Approved, Description = "A safe home for orphaned children, providing education and care.", CreatedAt = DateTime.UtcNow.AddDays(-10)
            };
            var org2 = new SocialHelpDonation.Models.Organisation
            {
                Name = "Serenity Old Age Home", Email = "hello@serenityhome.org", PasswordHash = passwordService.HashPassword("Org@123"), Phone = "8765432109", Address = "45 Peaceful Lane, Suburb", OrgType = SocialHelpDonation.Models.OrgType.OldAgeHome, Status = SocialHelpDonation.Models.OrgStatus.Approved, Description = "Providing a loving community and medical support for senior citizens.", CreatedAt = DateTime.UtcNow.AddDays(-5)
            };
            var org3 = new SocialHelpDonation.Models.Organisation
            {
                Name = "Vision Blind School", Email = "info@visionblindschool.org", PasswordHash = passwordService.HashPassword("Org@123"), Phone = "7654321098", Address = "78 Light Blvd, Downtown", OrgType = SocialHelpDonation.Models.OrgType.BlindSchool, Status = SocialHelpDonation.Models.OrgStatus.Approved, Description = "Empowering visually impaired youth through specialized education and skills training.", CreatedAt = DateTime.UtcNow.AddDays(-2)
            };

            db.Organisations.AddRange(org1, org2, org3);
            db.SaveChanges(); // Save to generate IDs

            db.Requirements.AddRange(
                new SocialHelpDonation.Models.Requirement { OrganisationId = org1.Id, ItemType = "Books", QuantityNeeded = 100, Description = "Educational books for middle schoolers.", Status = SocialHelpDonation.Models.RequirementStatus.Open },
                new SocialHelpDonation.Models.Requirement { OrganisationId = org1.Id, ItemType = "Clothes", QuantityNeeded = 50, Description = "Winter jackets for kids aged 5-10.", Status = SocialHelpDonation.Models.RequirementStatus.Open },
                new SocialHelpDonation.Models.Requirement { OrganisationId = org2.Id, ItemType = "Medical Supplies", QuantityNeeded = 20, Description = "First aid kits and basic medications.", Status = SocialHelpDonation.Models.RequirementStatus.Open },
                new SocialHelpDonation.Models.Requirement { OrganisationId = org3.Id, ItemType = "Money", QuantityNeeded = 1000, Description = "Funds for purchasing braille equipment.", Status = SocialHelpDonation.Models.RequirementStatus.Open }
            );
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while creating the database.");
    }
}

// ─── Middleware ───────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
