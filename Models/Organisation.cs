using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SocialHelpDonation.Models
{
    public enum OrgType { Orphanage, BlindSchool, OldAgeHome, Other }
    public enum OrgStatus { Pending, Approved, Rejected }

    public class Organisation
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public OrgType OrgType { get; set; } = OrgType.Orphanage;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public OrgStatus Status { get; set; } = OrgStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Requirement> Requirements { get; set; } = new List<Requirement>();
        public ICollection<Donation> Donations { get; set; } = new List<Donation>();
    }
}
