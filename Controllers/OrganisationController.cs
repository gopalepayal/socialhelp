using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using SocialHelpDonation.Data;
using SocialHelpDonation.Models;
using SocialHelpDonation.Models.ViewModels;
using SocialHelpDonation.Hubs;

namespace SocialHelpDonation.Controllers
{
    public class OrganisationController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<ChatHub> _hubContext;

        public OrganisationController(ApplicationDbContext db, IHubContext<ChatHub> hubContext)
        {
            _db = db;
            _hubContext = hubContext;
        }

        private bool IsOrg() => HttpContext.Session.GetString("UserRole") == "Organisation";
        private int OrgId() => HttpContext.Session.GetInt32("OrgId") ?? 0;

        // ─── Dashboard ───────────────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            if (!IsOrg()) return RedirectToAction("OrgLogin", "Auth");
            var org = await _db.Organisations
                .Include(o => o.Requirements)
                .Include(o => o.Donations)
                .FirstOrDefaultAsync(o => o.Id == OrgId());
            return View(org);
        }

        // ─── Requirements ────────────────────────────────────────────────────────
        public async Task<IActionResult> Requirements()
        {
            if (!IsOrg()) return RedirectToAction("OrgLogin", "Auth");
            var reqs = await _db.Requirements
                .Where(r => r.OrganisationId == OrgId())
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(reqs);
        }

        [HttpGet]
        public IActionResult AddRequirement()
        {
            if (!IsOrg()) return RedirectToAction("OrgLogin", "Auth");
            return View(new CreateRequirementViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> AddRequirement(CreateRequirementViewModel model)
        {
            if (!IsOrg()) return RedirectToAction("OrgLogin", "Auth");
            if (!ModelState.IsValid) return View(model);

            var req = new Requirement
            {
                OrganisationId = OrgId(),
                ItemType = model.ItemType,
                Description = model.Description,
                QuantityNeeded = model.QuantityNeeded,
                Status = RequirementStatus.Open
            };

            // Map type-specific fields
            switch (model.ItemType)
            {
                case DonationType.Money:
                    req.Amount = model.Amount;
                    break;
                case DonationType.Food:
                    req.FoodType = model.FoodType;
                    req.MealType = model.MealType;
                    req.NumberOfPlates = model.NumberOfPlates;
                    break;
                case DonationType.Clothes:
                    req.ClothCategory = model.ClothCategory;
                    req.ClothType = model.ClothType;
                    req.Size = model.Size;
                    break;
                case DonationType.Books:
                    req.BookType = model.BookType;
                    break;
            }

            _db.Requirements.Add(req);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Requirement added successfully.";
            return RedirectToAction("Requirements");
        }

        [HttpPost]
        public async Task<IActionResult> CloseRequirement(int id)
        {
            if (!IsOrg()) return Unauthorized();
            var req = await _db.Requirements.FirstOrDefaultAsync(r => r.Id == id && r.OrganisationId == OrgId());
            if (req != null)
            {
                req.Status = RequirementStatus.Closed;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Requirement closed.";
            }
            return RedirectToAction("Requirements");
        }

        // ─── Donation Requests ────────────────────────────────────────────────────
        public async Task<IActionResult> DonationRequests()
        {
            if (!IsOrg()) return RedirectToAction("OrgLogin", "Auth");
            var donations = await _db.Donations
                .Include(d => d.Donor)
                .Where(d => d.OrganisationId == OrgId())
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
            return View(donations);
        }

        public async Task<IActionResult> DonationDetails(int id)
        {
            if (!IsOrg()) return RedirectToAction("OrgLogin", "Auth");
            var donation = await _db.Donations
                .Include(d => d.Donor)
                .Include(d => d.ChatMessages)
                .FirstOrDefaultAsync(d => d.Id == id && d.OrganisationId == OrgId());
            
            if (donation == null) return NotFound();
            return View(donation);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveDonation(int id, string? notes)
        {
            if (!IsOrg()) return Unauthorized();
            var donation = await _db.Donations.FirstOrDefaultAsync(d => d.Id == id && d.OrganisationId == OrgId());
            if (donation != null && donation.Status == DonationStatus.Pending)
            {
                donation.Status = DonationStatus.Approved;
                donation.OrgNotes = notes;
                donation.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                
                // Notify Donor
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Your donation (RCP-{donation.ReceiptNumber.Substring(donation.ReceiptNumber.Length-4)}) has been Approved!", "success");
            }
            return RedirectToAction("DonationRequests");
        }

        [HttpPost]
        public async Task<IActionResult> RejectDonation(int id, string? notes)
        {
            if (!IsOrg()) return Unauthorized();
            var donation = await _db.Donations.FirstOrDefaultAsync(d => d.Id == id && d.OrganisationId == OrgId());
            if (donation != null && donation.Status == DonationStatus.Pending)
            {
                donation.Status = DonationStatus.Rejected;
                donation.OrgNotes = notes;
                donation.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                
                // Notify Donor
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Your donation (RCP-{donation.ReceiptNumber.Substring(donation.ReceiptNumber.Length-4)}) has been Rejected.", "error");
            }
            return RedirectToAction("DonationRequests");
        }

        [HttpPost]
        public async Task<IActionResult> CompleteDonation(int id)
        {
            if (!IsOrg()) return Unauthorized();
            var donation = await _db.Donations.FirstOrDefaultAsync(d => d.Id == id && d.OrganisationId == OrgId());
            if (donation != null && donation.Status == DonationStatus.Approved)
            {
                donation.Status = DonationStatus.Completed;
                donation.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                
                // Notify Donor
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Donation (RCP-{donation.ReceiptNumber.Substring(donation.ReceiptNumber.Length-4)}) marked as Completed. Thank you!", "success");
            }
            return RedirectToAction("DonationRequests");
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePickupStatus(int id, string status)
        {
            if (!IsOrg()) return Unauthorized();
            var donation = await _db.Donations.FirstOrDefaultAsync(d => d.Id == id && d.OrganisationId == OrgId());
            if (donation != null && donation.IsPickupRequested)
            {
                donation.PickupStatus = status;
                donation.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                // Notify Donor
                await _hubContext.Clients.All.SendAsync("ReceiveNotification", $"Pickup status updated for RCP-{donation.ReceiptNumber.Substring(donation.ReceiptNumber.Length-4)}: {status}", "info");
            }
            return RedirectToAction("DonationDetails", new { id });
        }

        // ─── Public Profile ──────────────────────────────────────────────────────
        public async Task<IActionResult> Profile(int id)
        {
            var org = await _db.Organisations
                .Include(o => o.Requirements.Where(r => r.Status == RequirementStatus.Open))
                .FirstOrDefaultAsync(o => o.Id == id && o.Status == OrgStatus.Approved);
            if (org == null) return NotFound();
            return View(org);
        }
    }
}
