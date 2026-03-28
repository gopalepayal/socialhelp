using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialHelpDonation.Models
{
    public enum RequirementStatus { Open, Fulfilled, Closed }

    public class Requirement
    {
        [Key]
        public int Id { get; set; }

        public int OrganisationId { get; set; }
        [ForeignKey("OrganisationId")]
        public Organisation? Organisation { get; set; }

        public DonationType ItemType { get; set; } = DonationType.Money;

        [MaxLength(1000)]
        public string? Description { get; set; }

        // ─── Money ────────────────────────────────
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Amount { get; set; }

        // ─── Food ─────────────────────────────────
        [MaxLength(50)]
        public string? FoodType { get; set; }

        [MaxLength(50)]
        public string? MealType { get; set; }

        public int? NumberOfPlates { get; set; }

        // ─── Clothes ──────────────────────────────
        [MaxLength(20)]
        public string? ClothCategory { get; set; }

        [MaxLength(50)]
        public string? ClothType { get; set; }

        [MaxLength(50)]
        public string? ClothCondition { get; set; }

        [MaxLength(10)]
        public string? Size { get; set; }

        // ─── Books ────────────────────────────────
        [MaxLength(50)]
        public string? BookType { get; set; }

        // ─── Common ──────────────────────────────
        public int QuantityNeeded { get; set; } = 1;

        public RequirementStatus Status { get; set; } = RequirementStatus.Open;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
