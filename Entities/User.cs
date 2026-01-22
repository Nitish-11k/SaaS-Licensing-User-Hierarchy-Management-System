using SaasLicenseSystem.Api.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace SaasLicenseSystem.Api.Entities
{
    public class User : BaseEntity
    {
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public required string FullName { get; set; }
        public bool IsActive { get; set; } = true;

       
        public Guid? ParentId { get; set; }
        [ForeignKey("ParentId")]
        public User? Parent { get; set; }
        public ICollection<User> SubUsers { get; set; } = new List<User>();

       
        public Guid RoleId { get; set; }
        public Role Role { get; set; } = null!;

        public Guid? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
        public ICollection<LicenseAssignment> LicenseAssignments { get; set; } = new List<LicenseAssignment>();
    }
}