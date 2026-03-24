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

        [Required, MaxLength(200)]
        public string ItemType { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int QuantityNeeded { get; set; } = 1;

        public RequirementStatus Status { get; set; } = RequirementStatus.Open;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
