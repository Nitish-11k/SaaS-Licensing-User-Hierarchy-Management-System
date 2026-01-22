using System.ComponentModel.DataAnnotations;

namespace SaasLicenseSystem.Api.Entities
{
    public class Tenant : BaseEntity
    {
        public required string Name { get; set; }
        public required string OwnerEmail { get; set; }
        public bool IsActive { get; set; } = true;
    }
}