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
        private readonly IWebHostEnvironment _env;
        private readonly EmailService _email;

        public AuthController(ApplicationDbContext db, PasswordService pwd, IWebHostEnvironment env, EmailService email)
        {
            _db = db;
            _pwd = pwd;
            _env = env;
            _email = email;
        }

        // ─── Admin ──────────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult AdminLogin() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminLogin([Bind("Email,Password")] AdminLoginViewModel model)
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
                RegistrationNumber = model.RegistrationNumber,
                Description = model.Description,
                Status = OrgStatus.Pending
            };

            var verification = new OrganizationVerification
            {
                Organisation = org
            };

            // ─── Handle document uploads ──────────────────────────────────────────
            var docsDir = Path.Combine(_env.WebRootPath, "uploads", "documents");
            if (!Directory.Exists(docsDir)) Directory.CreateDirectory(docsDir);

            // 1. Registration Certificate
            if (model.DocumentFile != null && model.DocumentFile.Length > 0)
            {
                var fileName = "CERT_" + Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(model.DocumentFile.FileName);
                using (var stream = new FileStream(Path.Combine(docsDir, fileName), FileMode.Create))
                {
                    await model.DocumentFile.CopyToAsync(stream);
                }
                verification.CertificateFilePath = "/uploads/documents/" + fileName;
            }

            // 2. ID Proof
            if (model.IdProofFile != null && model.IdProofFile.Length > 0)
            {
                var fileName = "ID_" + Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(model.IdProofFile.FileName);
                using (var stream = new FileStream(Path.Combine(docsDir, fileName), FileMode.Create))
                {
                    await model.IdProofFile.CopyToAsync(stream);
                }
                verification.IdProofFilePath = "/uploads/documents/" + fileName;
            }

            // 3. Address Proof
            if (model.AddressProofFile != null && model.AddressProofFile.Length > 0)
            {
                var fileName = "ADDR_" + Guid.NewGuid().ToString().Substring(0, 8) + Path.GetExtension(model.AddressProofFile.FileName);
                using (var stream = new FileStream(Path.Combine(docsDir, fileName), FileMode.Create))
                {
                    await model.AddressProofFile.CopyToAsync(stream);
                }
                verification.AddressProofFilePath = "/uploads/documents/" + fileName;
            }

            // ─── Handle organisation image upload (Additive field to Org) ──────────
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var ext = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                    var imgDir = Path.Combine(_env.WebRootPath, "images", "orgs");
                    if (!Directory.Exists(imgDir)) Directory.CreateDirectory(imgDir);

                    var imgName = Guid.NewGuid().ToString() + ext;
                    using (var stream = new FileStream(Path.Combine(imgDir, imgName), FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(stream);
                    }
                    org.ImagePath = "/images/orgs/" + imgName;
                }

            _db.Organisations.Add(org);
            _db.OrganizationVerifications.Add(verification);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (ex.InnerException != null && ex.InnerException.Message.Contains("Duplicate entry"))
            {
                ModelState.AddModelError("Email", "Email already registered.");
                return View(model);
            }

            TempData["Success"] = "Your organization is under verification. Admin will review your documents before approval.";
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
                ModelState.AddModelError("", "Your account is pending admin verification.");
                return View(model);
            }
            if (org.Status == OrgStatus.Rejected)
            {
                ModelState.AddModelError("", "Your verification was rejected. Please contact admin.");
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
        public IActionResult DonorLogin(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DonorLogin(DonorLoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var donor = await _db.Donors.FirstOrDefaultAsync(d => d.Email == model.Email);
            if (donor == null || !_pwd.VerifyPassword(model.Password, donor.PasswordHash))
            {
                ModelState.AddModelError("", "Invalid email or password.");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            HttpContext.Session.SetInt32("DonorId", donor.Id);
            HttpContext.Session.SetString("DonorName", donor.Name);
            HttpContext.Session.SetString("UserRole", "Donor");

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Dashboard", "Donor");
        }

        // ─── Password Reset ──────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            // Find user in any of the 3 tables
            var admin = await _db.Admins.FirstOrDefaultAsync(u => u.Email == model.Email);
            var org = await _db.Organisations.FirstOrDefaultAsync(u => u.Email == model.Email);
            var donor = await _db.Donors.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (admin == null && org == null && donor == null)
            {
                ModelState.AddModelError("Email", "Email not found in our system.");
                return View(model);
            }

            return RedirectToAction("ResetPassword", new { email = model.Email });
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation() => View();

        [HttpGet]
        public IActionResult ResetPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("ForgotPassword");
            }
            return View(new ResetPasswordViewModel { Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var admin = await _db.Admins.FirstOrDefaultAsync(u => u.Email == model.Email);
            var org = await _db.Organisations.FirstOrDefaultAsync(u => u.Email == model.Email);
            var donor = await _db.Donors.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (admin == null && org == null && donor == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View(model);
            }

            var newHash = _pwd.HashPassword(model.NewPassword);
            string roleLoginAction = "DonorLogin";

            if (admin != null) { admin.PasswordHash = newHash; roleLoginAction = "AdminLogin"; }
            if (org != null) { org.PasswordHash = newHash; roleLoginAction = "OrgLogin"; }
            if (donor != null) { donor.PasswordHash = newHash; }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Password updated successfully";
            return RedirectToAction(roleLoginAction);
        }

        // ─── Logout ──────────────────────────────────────────────────────────────
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
