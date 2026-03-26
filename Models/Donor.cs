using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SocialHelpDonation.Models
{
    public class Donor
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(255)]
        public string? ResetToken { get; set; }

        public DateTime? ResetTokenExpiry { get; set; }

        public ICollection<Donation> Donations { get; set; } = new List<Donation>();
    }
}
