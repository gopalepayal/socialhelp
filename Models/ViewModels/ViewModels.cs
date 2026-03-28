using SocialHelpDonation.Models;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

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

        [Required, MaxLength(100)]
        [Display(Name = "Registration Number (Government ID)")]
        public string RegistrationNumber { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Please upload a Registration Certificate (PDF or Image).")]
        [Display(Name = "Registration Certificate")]
        public IFormFile? DocumentFile { get; set; }

        [Required(ErrorMessage = "Please upload a valid ID Proof.")]
        [Display(Name = "ID Proof")]
        public IFormFile? IdProofFile { get; set; }

        [Required(ErrorMessage = "Please upload a valid Address Proof.")]
        [Display(Name = "Address Proof")]
        public IFormFile? AddressProofFile { get; set; }

        [Display(Name = "Organisation Image")]
        public IFormFile? ImageFile { get; set; }
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

        [Required]
        public DonationType DonationType { get; set; } = DonationType.Money;

        [MaxLength(1000)]
        public string? Description { get; set; }

        // Money
        [Range(1, 10000000)]
        public decimal? Amount { get; set; }

        // Food
        [MaxLength(50)]
        public string? FoodType { get; set; }

        [MaxLength(50)]
        public string? MealType { get; set; }

        [Range(1, 10000)]
        public int? NumberOfPlates { get; set; }

        // Clothes
        [MaxLength(20)]
        public string? ClothCategory { get; set; }

        [MaxLength(50)]
        public string? ClothType { get; set; }

        [MaxLength(50)]
        public string? ClothCondition { get; set; }

        [MaxLength(10)]
        public string? Size { get; set; }

        [Range(1, 100000)]
        public int Quantity { get; set; } = 1;

        // Books
        [MaxLength(50)]
        public string? BookType { get; set; }

        public bool IsPickupRequested { get; set; } = false;

        [MaxLength(500)]
        public string? PickupAddress { get; set; }

        [MaxLength(500)]
        public string? PickupLocation { get; set; }

        public double? PickupLatitude { get; set; }
        public double? PickupLongitude { get; set; }
    }

    // ─── Requirement ViewModels ────────────────────────────────────────────────
    public class CreateRequirementViewModel
    {
        [Required]
        public DonationType ItemType { get; set; } = DonationType.Money;

        [MaxLength(1000)]
        public string? Description { get; set; }

        // Money
        [Range(1, 10000000)]
        public decimal? Amount { get; set; }

        // Food
        [MaxLength(50)]
        public string? FoodType { get; set; }

        [MaxLength(50)]
        public string? MealType { get; set; }

        [Range(1, 10000)]
        public int? NumberOfPlates { get; set; }

        // Clothes
        [MaxLength(20)]
        public string? ClothCategory { get; set; }

        [MaxLength(50)]
        public string? ClothType { get; set; }

        [MaxLength(50)]
        public string? ClothCondition { get; set; }

        [MaxLength(10)]
        public string? Size { get; set; }

        // Books
        [MaxLength(50)]
        public string? BookType { get; set; }

        [Range(1, 100000)]
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
        public int ApprovedDonations { get; set; }
        public int PendingDonations { get; set; }
    }
}
