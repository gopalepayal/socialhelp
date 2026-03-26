using Microsoft.EntityFrameworkCore;
using SocialHelpDonation.Models;

namespace SocialHelpDonation.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Admin> Admins { get; set; }
        public DbSet<Organisation> Organisations { get; set; }
        public DbSet<Donor> Donors { get; set; }
        public DbSet<Donation> Donations { get; set; }
        public DbSet<Requirement> Requirements { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure enums as strings for readability
            modelBuilder.Entity<Organisation>()
                .Property(o => o.OrgType)
                .HasConversion<string>();

            modelBuilder.Entity<Organisation>()
                .Property(o => o.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Donation>()
                .Property(d => d.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Donation>()
                .Property(d => d.DonationType)
                .HasConversion<string>();

            modelBuilder.Entity<Requirement>()
                .Property(r => r.Status)
                .HasConversion<string>();

            modelBuilder.Entity<Requirement>()
                .Property(r => r.ItemType)
                .HasConversion<string>();

            // Unique constraints
            modelBuilder.Entity<Admin>()
                .HasIndex(a => a.Email).IsUnique();

            modelBuilder.Entity<Organisation>()
                .HasIndex(o => o.Email).IsUnique();

            modelBuilder.Entity<Donor>()
                .HasIndex(d => d.Email).IsUnique();

            modelBuilder.Entity<Donation>()
                .HasIndex(d => d.ReceiptNumber).IsUnique();
        }
    }
}
