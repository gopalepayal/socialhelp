using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SocialHelpDonation.Models
{
    public enum OrgType { Orphanage, BlindSchool, OldAgeHome, NGO }
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

        [Required, MaxLength(100)]
        public string RegistrationNumber { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ProofFilePath { get; set; }

        [MaxLength(500)]
        public string? IdProofFilePath { get; set; }

        [MaxLength(500)]
        public string? AddressProofFilePath { get; set; }

        [MaxLength(500)]
        public string? ImagePath { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public OrgStatus Status { get; set; } = OrgStatus.Pending;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? ResetToken { get; set; }

        public DateTime? ResetTokenExpiry { get; set; }

        public ICollection<Requirement> Requirements { get; set; } = new List<Requirement>();
        public ICollection<Donation> Donations { get; set; } = new List<Donation>();
    }
}
