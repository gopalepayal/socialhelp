using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialHelpDonation.Data;
using SocialHelpDonation.Models;
using SocialHelpDonation.Models.ViewModels;

namespace SocialHelpDonation.Controllers
{
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }

        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("AdminLogin", "Auth");
            
            ViewBag.PendingOrgs = await _db.Organisations.CountAsync(o => o.Status == OrgStatus.Pending);
            ViewBag.AllDonations = await _db.Donations.CountAsync();
            ViewBag.TotalDonors = await _db.Donors.CountAsync();
            
            return View();
        }

        public async Task<IActionResult> PendingOrganisations()
        {
            if (!IsAdmin()) return RedirectToAction("AdminLogin", "Auth");

            var pendingVerifications = await _db.OrganizationVerifications
                .Include(v => v.Organisation)
                .Where(v => v.Organisation != null && v.Organisation.Status == OrgStatus.Pending)
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            return View(pendingVerifications);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveOrganisation(int id)
        {
            if (!IsAdmin()) return RedirectToAction("AdminLogin", "Auth");

            var verification = await _db.OrganizationVerifications
                .Include(v => v.Organisation)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (verification != null)
            {
                if (verification.Organisation != null) verification.Organisation.Status = OrgStatus.Approved;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Organisation '{verification.Organisation?.Name}' has been approved.";
            }

            return RedirectToAction(nameof(PendingOrganisations));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectOrganisation(int id)
        {
            if (!IsAdmin()) return RedirectToAction("AdminLogin", "Auth");

            var verification = await _db.OrganizationVerifications
                .Include(v => v.Organisation)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (verification != null)
            {
                if (verification.Organisation != null) verification.Organisation.Status = OrgStatus.Rejected;
                await _db.SaveChangesAsync();
                TempData["Error"] = $"Organisation '{verification.Organisation?.Name}' has been rejected.";
            }

            return RedirectToAction(nameof(PendingOrganisations));
        }

        public async Task<IActionResult> Organisations()
        {
            if (!IsAdmin()) return RedirectToAction("AdminLogin", "Auth");
            var orgs = await _db.Organisations.OrderByDescending(o => o.CreatedAt).ToListAsync();
            return View(orgs);
        }

        public async Task<IActionResult> Donors()
        {
            if (!IsAdmin()) return RedirectToAction("AdminLogin", "Auth");
            var donors = await _db.Donors.OrderByDescending(d => d.CreatedAt).ToListAsync();
            return View(donors);
        }

        public async Task<IActionResult> Reports()
        {
            if (!IsAdmin()) return RedirectToAction("AdminLogin", "Auth");

            var model = new AdminReportViewModel
            {
                TotalOrgs = await _db.Organisations.CountAsync(),
                PendingOrgs = await _db.Organisations.CountAsync(o => o.Status == OrgStatus.Pending),
                ApprovedOrgs = await _db.Organisations.CountAsync(o => o.Status == OrgStatus.Approved),
                TotalDonors = await _db.Donors.CountAsync(),
                TotalDonations = await _db.Donations.CountAsync(),
                ApprovedDonations = await _db.Donations.CountAsync(d => d.Status == DonationStatus.Approved || d.Status == DonationStatus.Completed),
                PendingDonations = await _db.Donations.CountAsync(d => d.Status == DonationStatus.Pending)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveOrg(int id)
        {
            if (!IsAdmin()) return RedirectToAction("AdminLogin", "Auth");

            var org = await _db.Organisations.FindAsync(id);
            if (org != null)
            {
                org.Status = OrgStatus.Approved;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Organisation '{org.Name}' approved.";
            }
            return RedirectToAction(nameof(Organisations));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectOrg(int id)
        {
            if (!IsAdmin()) return RedirectToAction("AdminLogin", "Auth");

            var org = await _db.Organisations.FindAsync(id);
            if (org != null)
            {
                org.Status = OrgStatus.Rejected;
                await _db.SaveChangesAsync();
                TempData["Error"] = $"Organisation '{org.Name}' rejected.";
            }
            return RedirectToAction(nameof(Organisations));
        }

        public async Task<IActionResult> Donations()
        {
            if (!IsAdmin()) return RedirectToAction("AdminLogin", "Auth");
            var donations = await _db.Donations
                .Include(d => d.Donor)
                .Include(d => d.Organisation)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
            return View(donations);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDonation(int id)
        {
            if (!IsAdmin()) return RedirectToAction("AdminLogin", "Auth");

            var donation = await _db.Donations.FindAsync(id);
            if (donation != null && donation.Status == DonationStatus.Pending)
            {
                donation.Status = DonationStatus.Approved;
                donation.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Donation {donation.ReceiptNumber} has been approved.";
            }

            return RedirectToAction(nameof(Donations));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDonation(int id)
        {
            if (!IsAdmin()) return RedirectToAction("AdminLogin", "Auth");

            var donation = await _db.Donations.FindAsync(id);
            if (donation != null)
            {
                _db.Donations.Remove(donation);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Donation record deleted.";
            }

            return RedirectToAction(nameof(Donations));
        }
    }
}
