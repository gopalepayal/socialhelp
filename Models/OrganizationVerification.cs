using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SocialHelpDonation.Models
{

    public class OrganizationVerification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrganisationId { get; set; }

        [ForeignKey("OrganisationId")]
        public Organisation? Organisation { get; set; }

        [MaxLength(500)]
        public string? CertificateFilePath { get; set; }

        [MaxLength(500)]
        public string? IdProofFilePath { get; set; }

        [MaxLength(500)]
        public string? AddressProofFilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(1000)]
        public string? AdminRemarks { get; set; }
    }
}
