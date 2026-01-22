using Microsoft.EntityFrameworkCore;
using SaasLicenseSystem.Api.Entities;

namespace SaasLicenseSystem.Api.Data
{
    public class AppDbContext : DbContext
    {
        private readonly Guid _currentTenantId; 
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<License> Licenses { get; set; }
        public DbSet<LicenseAssignment> LicenseAssignments { get; set; }
        public DbSet<Machine> Machines { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Parent)
                .WithMany(u => u.SubUsers)
                .HasForeignKey(u => u.ParentId)
                .OnDelete(DeleteBehavior.Restrict); 

            modelBuilder.Entity<UserGroup>()
                .HasKey(ug => new { ug.UserId, ug.GroupId });

            modelBuilder.Entity<UserGroup>()
                .HasOne(ug => ug.User)
                .WithMany(u => u.UserGroups)
                .HasForeignKey(ug => ug.UserId);

            modelBuilder.Entity<UserGroup>()
                .HasOne(ug => ug.Group)
                .WithMany(g => g.UserGroups)
                .HasForeignKey(ug => ug.GroupId);

            modelBuilder.Entity<Machine>()
                .HasOne(m => m.LicenseAssignment)
                .WithMany(la => la.Machines)
                .HasForeignKey(m => m.LicenseAssignmentId);

        }
    }
}