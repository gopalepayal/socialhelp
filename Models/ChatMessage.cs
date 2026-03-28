using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialHelpDonation.Models
{
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        public int DonationId { get; set; }
        [ForeignKey("DonationId")]
        public Donation? Donation { get; set; }

        [Required]
        public string SenderId { get; set; } = string.Empty; // Store User ID (DonorId or OrgId)

        [Required]
        public string SenderRole { get; set; } = string.Empty; // "Donor" or "Organisation"

        [Required]
        public string Message { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
