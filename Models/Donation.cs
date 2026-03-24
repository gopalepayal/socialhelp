using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialHelpDonation.Models
{
    public enum DonationStatus { Pending, Accepted, Rejected, Completed }

    public class Donation
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(20)]
        public string ReceiptNumber { get; set; } = string.Empty;

        public int DonorId { get; set; }
        [ForeignKey("DonorId")]
        public Donor? Donor { get; set; }

        public int OrganisationId { get; set; }
        [ForeignKey("OrganisationId")]
        public Organisation? Organisation { get; set; }

        [Required, MaxLength(200)]
        public string ItemType { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int Quantity { get; set; } = 1;

        public DonationStatus Status { get; set; } = DonationStatus.Pending;

        [MaxLength(500)]
        public string? OrgNotes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
