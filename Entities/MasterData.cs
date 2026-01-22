using System.Collections.Generic;
using SaasLicenseSystem.Api.Entities;

namespace SaasLicenseSystem.Api.Entities
{
    public class Role : BaseEntity
    {
        public required string Name { get; set; }
        public string? Description { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
    }

    public class Department : BaseEntity
    {
        public required string Name { get; set; }
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}