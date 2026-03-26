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
builder.Services.AddScoped<EmailService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// ─── Drop & Recreate DB to apply new schema ───────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        // Create database if it doesn't exist (preserves existing data)
        db.Database.EnsureCreated();

        // Safely add ImagePath column if it doesn't exist yet
        try
        {
            db.Database.ExecuteSqlRaw("ALTER TABLE Organisations ADD COLUMN ImagePath VARCHAR(500) NULL");
        }
        catch { /* Column already exists, ignore */ }

        // Safely add ResetToken and ResetTokenExpiry columns
        try { db.Database.ExecuteSqlRaw("ALTER TABLE Admins ADD COLUMN ResetToken VARCHAR(255) NULL"); } catch { }
        try { db.Database.ExecuteSqlRaw("ALTER TABLE Admins ADD COLUMN ResetTokenExpiry DATETIME(6) NULL"); } catch { }
        
        try { db.Database.ExecuteSqlRaw("ALTER TABLE Donors ADD COLUMN ResetToken VARCHAR(255) NULL"); } catch { }
        try { db.Database.ExecuteSqlRaw("ALTER TABLE Donors ADD COLUMN ResetTokenExpiry DATETIME(6) NULL"); } catch { }

        try { db.Database.ExecuteSqlRaw("ALTER TABLE Organisations ADD COLUMN ResetToken VARCHAR(255) NULL"); } catch { }
        try { db.Database.ExecuteSqlRaw("ALTER TABLE Organisations ADD COLUMN ResetTokenExpiry DATETIME(6) NULL"); } catch { }

        // Seed default admins only if empty
        if (!db.Admins.Any())
        {
            var passwordService = scope.ServiceProvider.GetRequiredService<PasswordService>();
            db.Admins.Add(new SocialHelpDonation.Models.Admin { Name = "Super Admin", Email = "admin@donationsystem.com", PasswordHash = passwordService.HashPassword("Admin@123") });
            db.Admins.Add(new SocialHelpDonation.Models.Admin { Name = "Payal Admin", Email = "payalgopale449@gmail.com", PasswordHash = passwordService.HashPassword("Admin@123") });
            db.SaveChanges();
        }

        // Seed Sample Organisations only if empty
        if (!db.Organisations.Any())
        {
            var passwordService = scope.ServiceProvider.GetRequiredService<PasswordService>();
            var org1 = new SocialHelpDonation.Models.Organisation
            {
                Name = "Triya Orphanage",
                Email = "contact@triyaorphanage.org",
                PasswordHash = passwordService.HashPassword("Org@123"),
                Phone = "9876543210",
                Address = "123 Hope Street, City",
                OrgType = SocialHelpDonation.Models.OrgType.Orphanage,
                RegistrationNumber = "NGO/2024/MH/00101",
                Status = SocialHelpDonation.Models.OrgStatus.Approved,
                Description = "A safe home for orphaned children, providing education and care.",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };
            var org2 = new SocialHelpDonation.Models.Organisation
            {
                Name = "Serenity Old Age Home",
                Email = "hello@serenityhome.org",
                PasswordHash = passwordService.HashPassword("Org@123"),
                Phone = "8765432109",
                Address = "45 Peaceful Lane, Suburb",
                OrgType = SocialHelpDonation.Models.OrgType.OldAgeHome,
                RegistrationNumber = "NGO/2024/MH/00202",
                Status = SocialHelpDonation.Models.OrgStatus.Approved,
                Description = "Providing a loving community and medical support for senior citizens.",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };
            var org3 = new SocialHelpDonation.Models.Organisation
            {
                Name = "Vision Blind School",
                Email = "info@visionblindschool.org",
                PasswordHash = passwordService.HashPassword("Org@123"),
                Phone = "7654321098",
                Address = "78 Light Blvd, Downtown",
                OrgType = SocialHelpDonation.Models.OrgType.BlindSchool,
                RegistrationNumber = "NGO/2024/MH/00303",
                Status = SocialHelpDonation.Models.OrgStatus.Approved,
                Description = "Empowering visually impaired youth through specialized education and skills training.",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            };

            db.Organisations.AddRange(org1, org2, org3);
            db.SaveChanges();

            // Seed Requirements with type-specific fields only if organisations were just seeded
            if (!db.Requirements.Any())
            {
                db.Requirements.AddRange(
                    new SocialHelpDonation.Models.Requirement { OrganisationId = org1.Id, ItemType = SocialHelpDonation.Models.DonationType.Books, BookType = "Educational", QuantityNeeded = 100, Description = "Educational books for middle schoolers.", Status = SocialHelpDonation.Models.RequirementStatus.Open },
                    new SocialHelpDonation.Models.Requirement { OrganisationId = org1.Id, ItemType = SocialHelpDonation.Models.DonationType.Clothes, ClothCategory = "Kids", ClothType = "Jacket", Size = "M", QuantityNeeded = 50, Description = "Winter jackets for kids aged 5-10.", Status = SocialHelpDonation.Models.RequirementStatus.Open },
                    new SocialHelpDonation.Models.Requirement { OrganisationId = org2.Id, ItemType = SocialHelpDonation.Models.DonationType.Food, FoodType = "Veg", MealType = "Lunch", NumberOfPlates = 30, QuantityNeeded = 30, Description = "Daily lunch meals for 30 senior residents.", Status = SocialHelpDonation.Models.RequirementStatus.Open },
                    new SocialHelpDonation.Models.Requirement { OrganisationId = org3.Id, ItemType = SocialHelpDonation.Models.DonationType.Money, Amount = 50000, QuantityNeeded = 1, Description = "Funds for purchasing braille equipment.", Status = SocialHelpDonation.Models.RequirementStatus.Open }
                );
                db.SaveChanges();
            }
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
