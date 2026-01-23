using Microsoft.EntityFrameworkCore;
using SaasLicenseSystem.Api.Entities;
using System.Security.Claims;

namespace SaasLicenseSystem.Api.Data
{
    public class AppDbContext : DbContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        // Constructor now accepts HttpContextAccessor for getting the current Tenant
        public AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor) 
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
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

        // Helper to get current TenantId from the JWT Token
        private Guid GetCurrentTenantId()
        {
            var tenantIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("TenantId")?.Value;
            return Guid.TryParse(tenantIdClaim, out var tenantId) ? tenantId : Guid.Empty;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. YOUR EXISTING RELATIONSHIPS (Preserved)
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

            // 2. NEW GLOBAL QUERY FILTERS (For Multi-Tenancy Security)
            // This prevents a SuperAdmin from seeing data from other Organizations
            modelBuilder.Entity<User>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Department>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Group>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Role>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<License>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<LicenseAssignment>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<Machine>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
            modelBuilder.Entity<AuditLog>().HasQueryFilter(e => e.TenantId == GetCurrentTenantId());
        }

        // Automatically set TenantId when saving new records
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId != Guid.Empty)
            {
                foreach (var entry in ChangeTracker.Entries<BaseEntity>())
                {
                    if (entry.State == EntityState.Added)
                    {
                        entry.Entity.TenantId = tenantId;
                    }
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }
}