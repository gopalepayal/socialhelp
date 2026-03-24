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

        private bool IsAdmin() => HttpContext.Session.GetString("UserRole") == "Admin";

        // ─── Dashboard ───────────────────────────────────────────────────────────
        public async Task<IActionResult> Dashboard()
        {
            if (!IsAdmin()) return RedirectToAction("AdminLogin", "Auth");

            var report = new AdminReportViewModel
            {
                TotalOrgs = await _db.Organisations.CountAsync(),
                PendingOrgs = await _db.Organisations.CountAsync(o => o.Status == OrgStatus.Pending),
                ApprovedOrgs = await _db.Organisations.CountAsync(o => o.Status == OrgStatus.Approved),
                TotalDonors = await _db.Donors.CountAsync(),
                TotalDonations = await _db.Donations.CountAsync(),
                AcceptedDonations = await _db.Donations.CountAsync(d => d.Status == DonationStatus.Accepted),
                PendingDonations = await _db.Donations.CountAsync(d => d.Status == DonationStatus.Pending)
            };
            return View(report);
        }

        // ─── Manage Organisations ────────────────────────────────────────────────
        public async Task<IActionResult> Organisations()
        {
            if (!IsAdmin()) return RedirectToAction("AdminLogin", "Auth");
            var orgs = await _db.Organisations.OrderByDescending(o => o.CreatedAt).ToListAsync();
            return View(orgs);
        }

        [HttpPost]
        public async Task<IActionResult> ApproveOrg(int id)
        {
            if (!IsAdmin()) return Unauthorized();
            var org = await _db.Organisations.FindAsync(id);
            if (org != null)
            {
                org.Status = OrgStatus.Approved;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Organisation '{org.Name}' approved.";
            }
            return RedirectToAction("Organisations");
        }

        [HttpPost]
        public async Task<IActionResult> RejectOrg(int id)
        {
            if (!IsAdmin()) return Unauthorized();
            var org = await _db.Organisations.FindAsync(id);
            if (org != null)
            {
                org.Status = OrgStatus.Rejected;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Organisation '{org.Name}' rejected.";
            }
            return RedirectToAction("Organisations");
        }

        // ─── View Users ──────────────────────────────────────────────────────────
        public async Task<IActionResult> Donors()
        {
            if (!IsAdmin()) return RedirectToAction("AdminLogin", "Auth");
            var donors = await _db.Donors.OrderByDescending(d => d.CreatedAt).ToListAsync();
            return View(donors);
        }

        // ─── View Donations ──────────────────────────────────────────────────────
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

        // ─── Reports ─────────────────────────────────────────────────────────────
        public async Task<IActionResult> Reports()
        {
            if (!IsAdmin()) return RedirectToAction("AdminLogin", "Auth");
            var report = new AdminReportViewModel
            {
                TotalOrgs = await _db.Organisations.CountAsync(),
                PendingOrgs = await _db.Organisations.CountAsync(o => o.Status == OrgStatus.Pending),
                ApprovedOrgs = await _db.Organisations.CountAsync(o => o.Status == OrgStatus.Approved),
                TotalDonors = await _db.Donors.CountAsync(),
                TotalDonations = await _db.Donations.CountAsync(),
                AcceptedDonations = await _db.Donations.CountAsync(d => d.Status == DonationStatus.Accepted),
                PendingDonations = await _db.Donations.CountAsync(d => d.Status == DonationStatus.Pending)
            };
            return View(report);
        }
    }
}
