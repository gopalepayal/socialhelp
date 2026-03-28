using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using SocialHelpDonation.Data;
using SocialHelpDonation.Models;
using SocialHelpDonation.Models.ViewModels;
using SocialHelpDonation.Hubs;

namespace SocialHelpDonation.Controllers
{
    public class DonorController : Controller
    {
        private readonly ApplicationDbContext _db;

        private readonly IHubContext<ChatHub> _hubContext;

        public DonorController(ApplicationDbContext db, IHubContext<ChatHub> hubContext)
        {
            _db = db;
            _hubContext = hubContext;
        }

        private bool IsDonor() => HttpContext.Session.GetString("UserRole") == "Donor";
        private int DonorId() => HttpContext.Session.GetInt32("DonorId") ?? 0;

        // ─── Dashboard ───────────────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            if (!IsDonor()) return RedirectToAction("DonorLogin", "Auth");
            var totalDonations = await _db.Donations.CountAsync(d => d.DonorId == DonorId());
            var approvedDonations = await _db.Donations.CountAsync(d => d.DonorId == DonorId() && (d.Status == DonationStatus.Approved || d.Status == DonationStatus.Completed));
            var pendingDonations = await _db.Donations.CountAsync(d => d.DonorId == DonorId() && d.Status == DonationStatus.Pending);

            string badge = "New Member";
            string badgeColor = "secondary";
            if (totalDonations >= 10) { badge = "Gold Donor"; badgeColor = "warning"; }
            else if (totalDonations >= 5) { badge = "Silver Donor"; badgeColor = "secondary"; }
            else if (totalDonations >= 1) { badge = "Bronze Donor"; badgeColor = "danger"; } // using danger as a bronze-ish fallback or custom

            ViewBag.TotalDonations = totalDonations;
            ViewBag.ApprovedDonations = approvedDonations;
            ViewBag.PendingDonations = pendingDonations;
            ViewBag.DonorBadge = badge;
            ViewBag.BadgeColor = badgeColor;

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

            var donorId = DonorId();
            var pastDonatedOrgTypes = await _db.Donations
                .Where(d => d.DonorId == donorId && d.Organisation != null)
                .Select(d => d.Organisation!.OrgType)
                .Distinct()
                .ToListAsync();

            var approvedOrgs = await _db.Organisations
                .Where(o => o.Status == OrgStatus.Approved)
                .ToListAsync();

            if (pastDonatedOrgTypes.Any())
            {
                ViewBag.FeaturedOrgs = approvedOrgs
                    .OrderByDescending(o => pastDonatedOrgTypes.Contains(o.OrgType))
                    .ThenBy(x => Guid.NewGuid())
                    .Take(3)
                    .ToList();
            }
            else
            {
                ViewBag.FeaturedOrgs = approvedOrgs
                    .OrderBy(x => Guid.NewGuid())
                    .Take(3)
                    .ToList();
            }

            return View(donations);
        }

        // ─── Browse Organisations ─────────────────────────────────────────────────
        public async Task<IActionResult> BrowseOrganisations(string? search, string? type)
        {


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
            if (!IsDonor()) return RedirectToAction("DonorLogin", "Auth", new { returnUrl = $"/Donor/Donate?orgId={orgId}" });
            var org = await _db.Organisations.FindAsync(orgId);
            if (org == null || org.Status != OrgStatus.Approved) return NotFound();

            var model = new CreateDonationViewModel { OrganisationId = orgId, DonationType = DonationType.Money };
            ViewBag.OrgName = org.Name;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Donate(CreateDonationViewModel model)
        {
            if (!IsDonor()) return RedirectToAction("DonorLogin", "Auth", new { returnUrl = $"/Donor/Donate?orgId={model.OrganisationId}" });

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
                Status = DonationStatus.Pending,
                IsPickupRequested = model.IsPickupRequested,
                PickupStatus = model.IsPickupRequested ? "Requested" : null,
                PickupAddress = model.IsPickupRequested ? model.PickupAddress : null,
                PickupLatitude = model.IsPickupRequested ? model.PickupLatitude : null,
                PickupLongitude = model.IsPickupRequested ? model.PickupLongitude : null
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
                    donation.ClothCondition = model.ClothCondition;
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

            // Notify Organisation (Broadcasting for now, can be refined with specific ConnectionId)
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"New donation received from {HttpContext.Session.GetString("DonorName")}!", "success");
            
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

        public async Task<IActionResult> DonationDetails(int id)
        {
            if (!IsDonor()) return RedirectToAction("DonorLogin", "Auth");
            var donation = await _db.Donations
                .Include(d => d.Organisation)
                .Include(d => d.ChatMessages)
                .FirstOrDefaultAsync(d => d.Id == id && d.DonorId == DonorId());
            
            if (donation == null) return NotFound();
            return View(donation);
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
