namespace SaasLicenseSystem.Api.Entities
{
    public class AuditLog : BaseEntity
    {
        public Guid PerformedByUserId { get; set; } 
        public string Action { get; set; } = string.Empty;
        public string EntityAffected { get; set; } = string.Empty; 
        public string? Details { get; set; } 
    }
}