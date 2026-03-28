using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocialHelpDonation.Data;
using SocialHelpDonation.Models;
using System.Threading.Tasks;
using System.Linq;

namespace SocialHelpDonation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ChatController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("{donationId}")]
        public async Task<IActionResult> GetMessages(int donationId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role)) return Unauthorized();

            var donation = await _db.Donations.FindAsync(donationId);
            if (donation == null) return NotFound();

            // Security Check
            if (role == "Donor" && HttpContext.Session.GetInt32("DonorId") != donation.DonorId) return Unauthorized();
            if (role == "Organisation" && HttpContext.Session.GetInt32("OrgId") != donation.OrganisationId) return Unauthorized();

            var messages = await _db.ChatMessages
                .Where(m => m.DonationId == donationId)
                .OrderBy(m => m.Timestamp)
                .Select(m => new
                {
                    m.Id,
                    m.SenderId,
                    m.SenderRole,
                    m.Message,
                    Timestamp = m.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
                })
                .ToListAsync();

            return Ok(messages);
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessage model)
        {
            var role = HttpContext.Session.GetString("UserRole");
            if (string.IsNullOrEmpty(role)) return Unauthorized();

            var donation = await _db.Donations.FindAsync(model.DonationId);
            if (donation == null) return NotFound();

            // Security Check
            if (role == "Donor" && HttpContext.Session.GetInt32("DonorId") != donation.DonorId) return Unauthorized();
            if (role == "Organisation" && HttpContext.Session.GetInt32("OrgId") != donation.OrganisationId) return Unauthorized();

            model.Timestamp = DateTime.UtcNow;
            _db.ChatMessages.Add(model);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                model.Id,
                model.SenderId,
                model.SenderRole,
                model.Message,
                Timestamp = model.Timestamp.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }
    }
}
