using Microsoft.AspNetCore.Mvc;
using SocialHelpDonation.Data;
using System.Diagnostics;

namespace SocialHelpDonation.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        public HomeController(ApplicationDbContext db) { _db = db; }

        public IActionResult Index()
        {
            ViewBag.TotalOrgs = _db.Organisations.Count(o => o.Status == SocialHelpDonation.Models.OrgStatus.Approved);
            ViewBag.TotalDonors = _db.Donors.Count();
            ViewBag.TotalDonations = _db.Donations.Count(d => d.Status == SocialHelpDonation.Models.DonationStatus.Accepted);
            return View();
        }

        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
