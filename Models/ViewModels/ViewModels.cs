using SocialHelpDonation.Models;
using System.ComponentModel.DataAnnotations;

namespace SocialHelpDonation.Models.ViewModels
{
    // ─── Auth ViewModels ───────────────────────────────────────────────────────
    public class AdminLoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class OrgRegisterViewModel
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public OrgType OrgType { get; set; } = OrgType.Orphanage;

        [MaxLength(1000)]
        public string? Description { get; set; }
    }

    public class OrgLoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class DonorRegisterViewModel
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }
    }

    public class DonorLoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    // ─── Donation ViewModels ───────────────────────────────────────────────────
    public class CreateDonationViewModel
    {
        [Required]
        public int OrganisationId { get; set; }

        [Required, MaxLength(200)]
        public string ItemType { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Range(1, 10000)]
        public int Quantity { get; set; } = 1;
    }

    // ─── Requirement ViewModels ────────────────────────────────────────────────
    public class CreateRequirementViewModel
    {
        [Required, MaxLength(200)]
        public string ItemType { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Range(1, 10000)]
        public int QuantityNeeded { get; set; } = 1;
    }

    // ─── Admin Report ViewModels ───────────────────────────────────────────────
    public class AdminReportViewModel
    {
        public int TotalOrgs { get; set; }
        public int PendingOrgs { get; set; }
        public int ApprovedOrgs { get; set; }
        public int TotalDonors { get; set; }
        public int TotalDonations { get; set; }
        public int AcceptedDonations { get; set; }
        public int PendingDonations { get; set; }
    }
}
