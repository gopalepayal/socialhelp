using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialHelpDonation.Data;
using SocialHelpDonation.Models;
using SocialHelpDonation.Models.ViewModels;

namespace SocialHelpDonation.Controllers
{
    public class DonorController : Controller
    {
        private readonly ApplicationDbContext _db;

        public DonorController(ApplicationDbContext db)
        {
            _db = db;
        }

        private bool IsDonor() => HttpContext.Session.GetString("UserRole") == "Donor";
        private int DonorId() => HttpContext.Session.GetInt32("DonorId") ?? 0;

        // ─── Dashboard ───────────────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            if (!IsDonor()) return RedirectToAction("DonorLogin", "Auth");
            
            var donations = await _db.Donations
                .Include(d => d.Organisation)
                .Where(d => d.DonorId == DonorId())
                .OrderByDescending(d => d.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.UrgentNeeds = await _db.Requirements
                .Include(r => r.Organisation)
                .Where(r => r.Status == RequirementStatus.Open && r.Organisation!.Status == OrgStatus.Approved)
                .OrderByDescending(r => r.CreatedAt)
                .Take(4)
                .ToListAsync();

            ViewBag.FeaturedOrgs = await _db.Organisations
                .Where(o => o.Status == OrgStatus.Approved)
                .OrderByDescending(o => o.CreatedAt)
                .Take(3)
                .ToListAsync();

            return View(donations);
        }

        // ─── Browse Organisations ─────────────────────────────────────────────────
        public async Task<IActionResult> BrowseOrganisations(string? search, string? type)
        {
            if (!IsDonor()) return RedirectToAction("DonorLogin", "Auth");

            var query = _db.Organisations
                .Include(o => o.Requirements.Where(r => r.Status == RequirementStatus.Open))
                .Where(o => o.Status == OrgStatus.Approved)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(o => o.Name.Contains(search) || (o.Address != null && o.Address.Contains(search)));

            if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<OrgType>(type, out var orgType))
                query = query.Where(o => o.OrgType == orgType);

            ViewBag.Search = search;
            ViewBag.Type = type;
            var orgs = await query.OrderBy(o => o.Name).ToListAsync();
            return View(orgs);
        }

        // ─── Send Donation ────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Donate(int orgId)
        {
            if (!IsDonor()) return RedirectToAction("DonorLogin", "Auth");
            var org = await _db.Organisations.FindAsync(orgId);
            if (org == null || org.Status != OrgStatus.Approved) return NotFound();

            var model = new CreateDonationViewModel { OrganisationId = orgId, DonationType = DonationType.Money };
            ViewBag.OrgName = org.Name;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Donate(CreateDonationViewModel model)
        {
            if (!IsDonor()) return RedirectToAction("DonorLogin", "Auth");

            // Type-specific validation
            if (model.DonationType == DonationType.Money && (model.Amount == null || model.Amount <= 0))
                ModelState.AddModelError("Amount", "Please enter a valid amount.");

            if (model.DonationType == DonationType.Food && (model.NumberOfPlates == null || model.NumberOfPlates <= 0))
                ModelState.AddModelError("NumberOfPlates", "Please enter number of plates/people.");

            if (model.DonationType == DonationType.Clothes && model.Quantity <= 0)
                ModelState.AddModelError("Quantity", "Please enter quantity.");

            if (model.DonationType == DonationType.Books && model.Quantity <= 0)
                ModelState.AddModelError("Quantity", "Please enter quantity.");

            if (!ModelState.IsValid)
            {
                var org = await _db.Organisations.FindAsync(model.OrganisationId);
                ViewBag.OrgName = org?.Name ?? "";
                return View(model);
            }

            var receipt = "RCP-" + DateTime.UtcNow.ToString("yyyyMMdd") + "-" + new Random().Next(1000, 9999);
            var donation = new Donation
            {
                ReceiptNumber = receipt,
                DonorId = DonorId(),
                OrganisationId = model.OrganisationId,
                DonationType = model.DonationType,
                Description = model.Description,
                Status = DonationStatus.Pending
            };

            switch (model.DonationType)
            {
                case DonationType.Money:
                    donation.Amount = model.Amount;
                    donation.Quantity = 1;
                    break;

                case DonationType.Food:
                    donation.FoodType = model.FoodType;
                    donation.MealType = model.MealType;
                    donation.NumberOfPlates = model.NumberOfPlates;
                    donation.Quantity = model.NumberOfPlates ?? 1;
                    break;

                case DonationType.Clothes:
                    donation.ClothCategory = model.ClothCategory;
                    donation.ClothType = model.ClothType;
                    donation.Size = model.Size;
                    donation.Quantity = model.Quantity;
                    break;

                case DonationType.Books:
                    donation.BookType = model.BookType;
                    donation.Quantity = model.Quantity;
                    break;
            }

            _db.Donations.Add(donation);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Donation request sent successfully!";
            return RedirectToAction("MyDonations");
        }

        // ─── My Donations ─────────────────────────────────────────────────────────
        public async Task<IActionResult> MyDonations()
        {
            if (!IsDonor()) return RedirectToAction("DonorLogin", "Auth");
            var donations = await _db.Donations
                .Include(d => d.Organisation)
                .Where(d => d.DonorId == DonorId())
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
            return View(donations);
        }

        // ─── Receipt ──────────────────────────────────────────────────────────────
        public async Task<IActionResult> Receipt(int id)
        {
            if (!IsDonor()) return RedirectToAction("DonorLogin", "Auth");
            var donation = await _db.Donations
                .Include(d => d.Donor)
                .Include(d => d.Organisation)
                .FirstOrDefaultAsync(d => d.Id == id && d.DonorId == DonorId());

            if (donation == null) return NotFound();
            if (donation.Status != DonationStatus.Approved && donation.Status != DonationStatus.Completed)
            {
                TempData["Error"] = "Receipt is only available for approved/completed donations.";
                return RedirectToAction("MyDonations");
            }
            return View(donation);
        }
    }
}
