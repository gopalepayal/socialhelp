using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialHelpDonation.Models
{
    public enum DonationStatus { Pending, Approved, Rejected, Completed }
    public enum DonationType { Money, Food, Clothes, Books }

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

        public DonationType DonationType { get; set; } = DonationType.Money;

        // ─── Money Fields ─────────────────────────
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Amount { get; set; }

        // ─── Food Fields ──────────────────────────
        [MaxLength(50)]
        public string? FoodType { get; set; }       // Veg / Non-Veg

        [MaxLength(50)]
        public string? MealType { get; set; }       // Breakfast / Lunch / Dinner

        public int? NumberOfPlates { get; set; }

        // ─── Clothes Fields ───────────────────────
        [MaxLength(20)]
        public string? ClothCategory { get; set; }  // Men / Women / Kids

        [MaxLength(50)]
        public string? ClothType { get; set; }      // Shirt, Pants, Blanket, etc.

        [MaxLength(50)]
        public string? ClothCondition { get; set; } // New / Used

        [MaxLength(10)]
        public string? Size { get; set; }           // S, M, L, XL (optional)

        // ─── Books Fields ─────────────────────────
        [MaxLength(50)]
        public string? BookType { get; set; }       // Educational / Story / Other

        // ─── Common Fields ────────────────────────
        public int Quantity { get; set; } = 1;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public DonationStatus Status { get; set; } = DonationStatus.Pending;

        [MaxLength(500)]
        public string? OrgNotes { get; set; }

        // ─── Pickup Additions ─────────────────────
        public bool IsPickupRequested { get; set; } = false;

        [MaxLength(50)]
        public string? PickupStatus { get; set; } // Pending, Accepted, On the Way, Delivered

        public DateTime? PickupDate { get; set; }
        
        [MaxLength(500)]
        public string? PickupAddress { get; set; }

        [MaxLength(500)]
        public string? PickupLocation { get; set; }

        public double? PickupLatitude { get; set; }
        public double? PickupLongitude { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    }
}
