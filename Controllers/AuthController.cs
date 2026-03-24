using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialHelpDonation.Data;
using SocialHelpDonation.Models;
using SocialHelpDonation.Models.ViewModels;
using SocialHelpDonation.Services;

namespace SocialHelpDonation.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly PasswordService _pwd;

        public AuthController(ApplicationDbContext db, PasswordService pwd)
        {
            _db = db;
            _pwd = pwd;
        }

        // ─── Admin ──────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult AdminLogin() => View();

        [HttpPost]
        public async Task<IActionResult> AdminLogin(AdminLoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var admin = await _db.Admins.FirstOrDefaultAsync(a => a.Email == model.Email);
            if (admin == null || !_pwd.VerifyPassword(model.Password, admin.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            HttpContext.Session.SetInt32("AdminId", admin.Id);
            HttpContext.Session.SetString("AdminName", admin.Name);
            HttpContext.Session.SetString("UserRole", "Admin");
            return RedirectToAction("Dashboard", "Admin");
        }

        // ─── Organisation ────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult OrgRegister() => View();

        [HttpPost]
        public async Task<IActionResult> OrgRegister(OrgRegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _db.Organisations.AnyAsync(o => o.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already registered.");
                return View(model);
            }

            var org = new Organisation
            {
                Name = model.Name,
                Email = model.Email,
                PasswordHash = _pwd.HashPassword(model.Password),
                Phone = model.Phone,
                Address = model.Address,
                OrgType = model.OrgType,
                Description = model.Description,
                Status = OrgStatus.Pending
            };
            _db.Organisations.Add(org);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("Duplicate entry"))
            {
                ModelState.AddModelError("Email", "Email already registered.");
                return View(model);
            }

            TempData["Success"] = "Registration successful! Please wait for admin approval.";
            return RedirectToAction("OrgLogin");
        }

        [HttpGet]
        public IActionResult OrgLogin() => View();

        [HttpPost]
        public async Task<IActionResult> OrgLogin(OrgLoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var org = await _db.Organisations.FirstOrDefaultAsync(o => o.Email == model.Email);
            if (org == null || !_pwd.VerifyPassword(model.Password, org.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            if (org.Status == OrgStatus.Pending)
            {
                ModelState.AddModelError("", "Your account is pending admin approval.");
                return View(model);
            }

            if (org.Status == OrgStatus.Rejected)
            {
                ModelState.AddModelError("", "Your account has been rejected. Contact admin.");
                return View(model);
            }

            HttpContext.Session.SetInt32("OrgId", org.Id);
            HttpContext.Session.SetString("OrgName", org.Name);
            HttpContext.Session.SetString("UserRole", "Organisation");
            return RedirectToAction("Dashboard", "Organisation");
        }

        // ─── Donor ───────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult DonorRegister() => View();

        [HttpPost]
        public async Task<IActionResult> DonorRegister(DonorRegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _db.Donors.AnyAsync(d => d.Email == model.Email))
            {
                ModelState.AddModelError("Email", "Email already registered.");
                return View(model);
            }

            var donor = new Donor
            {
                Name = model.Name,
                Email = model.Email,
                PasswordHash = _pwd.HashPassword(model.Password),
                Phone = model.Phone,
                Address = model.Address
            };
            _db.Donors.Add(donor);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("Duplicate entry"))
            {
                ModelState.AddModelError("Email", "Email already registered.");
                return View(model);
            }

            TempData["Success"] = "Registration successful! You can now log in.";
            return RedirectToAction("DonorLogin");
        }

        [HttpGet]
        public IActionResult DonorLogin() => View();

        [HttpPost]
        public async Task<IActionResult> DonorLogin(DonorLoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var donor = await _db.Donors.FirstOrDefaultAsync(d => d.Email == model.Email);
            if (donor == null || !_pwd.VerifyPassword(model.Password, donor.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                return View(model);
            }

            HttpContext.Session.SetInt32("DonorId", donor.Id);
            HttpContext.Session.SetString("DonorName", donor.Name);
            HttpContext.Session.SetString("UserRole", "Donor");
            return RedirectToAction("Dashboard", "Donor");
        }

        // ─── Logout ──────────────────────────────────────────────────────────────
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
