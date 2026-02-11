using HarvestHavenSecurePortal.Models;
using Microsoft.EntityFrameworkCore;

namespace HarvestHavenSecurePortal.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<MemberProfile> MemberProfiles => Set<MemberProfile>();
        public DbSet<PasswordArchive> PasswordArchives => Set<PasswordArchive>();
        public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();
        public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<MemberProfile>()
                .HasIndex(m => m.Email)
                .IsUnique();

            modelBuilder.Entity<PasswordArchive>()
                .HasIndex(p => new { p.MemberProfileId, p.CreatedAt });

            modelBuilder.Entity<PasswordResetRequest>()
                .HasIndex(r => new { r.MemberProfileId, r.ExpiresAt });

            modelBuilder.Entity<MemberProfile>()
                .Property(m => m.Email)
                .HasMaxLength(256);
        }
    }
}
