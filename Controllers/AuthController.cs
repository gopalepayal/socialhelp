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

            // Handle document upload
            if (model.DocumentFile != null && model.DocumentFile.Length > 0)
            {
                var uploadsDir = Path.Combine(_env.WebRootPath, "uploads", "documents");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(model.DocumentFile.FileName);
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await model.DocumentFile.CopyToAsync(stream);
                }
                org.ProofFilePath = "/uploads/documents/" + fileName;
            }

            // Handle organisation image upload
            if (model.ImageFile != null && model.ImageFile.Length > 0)
            {
                var allowed = new[] { ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(model.ImageFile.FileName).ToLowerInvariant();
                if (allowed.Contains(ext))
                {
                    var imgDir = Path.Combine(_env.WebRootPath, "images", "orgs");
                    if (!Directory.Exists(imgDir)) Directory.CreateDirectory(imgDir);

                    var imgName = Guid.NewGuid().ToString() + ext;
                    var imgPath = Path.Combine(imgDir, imgName);

                    using (var stream = new FileStream(imgPath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(stream);
                    }
                    org.ImagePath = "/images/orgs/" + imgName;
                }
            }

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
                // Don't reveal if email exists or not for security
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            var resetToken = Guid.NewGuid().ToString();
            var expiry = DateTime.UtcNow.AddMinutes(15);

            if (admin != null) { admin.ResetToken = resetToken; admin.ResetTokenExpiry = expiry; }
            if (org != null) { org.ResetToken = resetToken; org.ResetTokenExpiry = expiry; }
            if (donor != null) { donor.ResetToken = resetToken; donor.ResetTokenExpiry = expiry; }

            await _db.SaveChangesAsync();

            var resetLink = Url.Action("ResetPassword", "Auth", new { token = resetToken, email = model.Email }, Request.Scheme);
            var emailBody = $"<p>You requested a password reset.</p><p>Please <a href='{resetLink}'>click here</a> to reset your password.</p><p>This link will expire in 15 minutes.</p>";

            await _email.SendEmailAsync(model.Email, "Reset Your Password", emailBody);

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation() => View();

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Invalid password reset token.");
            }
            return View(new ResetPasswordViewModel { Token = token, Email = email });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var admin = await _db.Admins.FirstOrDefaultAsync(u => u.Email == model.Email && u.ResetToken == model.Token);
            var org = await _db.Organisations.FirstOrDefaultAsync(u => u.Email == model.Email && u.ResetToken == model.Token);
            var donor = await _db.Donors.FirstOrDefaultAsync(u => u.Email == model.Email && u.ResetToken == model.Token);

            if (admin == null && org == null && donor == null)
            {
                ModelState.AddModelError("", "Invalid or expired token.");
                return View(model);
            }

            bool isExpired = false;
            if (admin != null && admin.ResetTokenExpiry < DateTime.UtcNow) isExpired = true;
            if (org != null && org.ResetTokenExpiry < DateTime.UtcNow) isExpired = true;
            if (donor != null && donor.ResetTokenExpiry < DateTime.UtcNow) isExpired = true;

            if (isExpired)
            {
                ModelState.AddModelError("", "Token has expired. Please request a new password reset.");
                return View(model);
            }

            var newHash = _pwd.HashPassword(model.NewPassword);

            if (admin != null) { admin.PasswordHash = newHash; admin.ResetToken = null; admin.ResetTokenExpiry = null; }
            if (org != null) { org.PasswordHash = newHash; org.ResetToken = null; org.ResetTokenExpiry = null; }
            if (donor != null) { donor.PasswordHash = newHash; donor.ResetToken = null; donor.ResetTokenExpiry = null; }

            await _db.SaveChangesAsync();

            return RedirectToAction("ResetPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation() => View();

        // ─── Logout ──────────────────────────────────────────────────────────────
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
