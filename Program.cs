using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using SocialHelpDonation.Data;
using SocialHelpDonation.Services;
using SocialHelpDonation.Hubs;

// Load .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// ─── Read DB credentials from environment ────────────────────────────────────
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "5432";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "postgres";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "postgres";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";

var connectionString = $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPassword};SSL Mode=Require;Trust Server Certificate=true;Pooling=false;";

// ─── Supabase API credentials ─────────────────────────────────────────────────
var supabaseUrl  = Environment.GetEnvironmentVariable("SUPABASE_URL")      ?? "";
var supabaseKey  = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") ?? "";
// These can be accessed from controllers via IConfiguration["SUPABASE_URL"] etc.
builder.Configuration["SUPABASE_URL"]      = supabaseUrl;
builder.Configuration["SUPABASE_ANON_KEY"] = supabaseKey;

// ─── Services ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<EmailService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();


var app = builder.Build();

// ─── Seed data (Supabase tables already created via SQL Editor) ───────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        // Test connection by running a lightweight query
        db.Database.ExecuteSqlRaw("SELECT 1");
        startupLogger.LogInformation("✅ Supabase PostgreSQL connection successful.");

        // Seed default admins only if empty
        if (!db.Admins.Any())
        {
            var passwordService = scope.ServiceProvider.GetRequiredService<PasswordService>();
            db.Admins.Add(new SocialHelpDonation.Models.Admin { Name = "Super Admin", Email = "admin@donationsystem.com", PasswordHash = passwordService.HashPassword("Admin@123") });
            db.Admins.Add(new SocialHelpDonation.Models.Admin { Name = "Payal Admin", Email = "payalgopale449@gmail.com", PasswordHash = passwordService.HashPassword("Admin@123") });
            db.SaveChanges();
            startupLogger.LogInformation("✅ Admin seed data inserted.");
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
                Latitude = 19.0760, Longitude = 72.8777,
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
                Latitude = 19.1136, Longitude = 72.8697,
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
                Latitude = 19.0178, Longitude = 72.8478,
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
            startupLogger.LogInformation("✅ Organisation and requirement seed data inserted.");
        }
        else
        {
            // Update existing organisations with coordinates if they were created before this update
            var orgs = db.Organisations.ToList();
            bool updated = false;
            foreach(var o in orgs) {
                if(!o.Latitude.HasValue) {
                    if(o.Name.Contains("Triya")) { o.Latitude = 19.0760; o.Longitude = 72.8777; }
                    else if(o.Name.Contains("Serenity")) { o.Latitude = 19.1136; o.Longitude = 72.8697; }
                    else if(o.Name.Contains("Vision")) { o.Latitude = 19.0178; o.Longitude = 72.8478; }
                    updated = true;
                }
            }
            if(updated) { db.SaveChanges(); startupLogger.LogInformation("✅ Updated coordinates for existing organisations."); }
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

app.MapHub<ChatHub>("/chatHub");

app.Run();
