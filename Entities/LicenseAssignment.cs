namespace SaasLicenseSystem.Api.Entities
{
    public class LicenseAssignment : BaseEntity
    {
        public Guid LicenseId { get; set; }
        public License License { get; set; } = null!;

        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

        public ICollection<Machine> Machines { get; set; } = new List<Machine>();
    }

    public class Machine : BaseEntity
    {
        public required string HardwareId { get; set; } 
        public string? MachineName { get; set; }
        public string? OperatingSystem { get; set; }
        public string? IpAddress { get; set; }
        public DateTime LastActive { get; set; }

        public Guid LicenseAssignmentId { get; set; }
        public LicenseAssignment LicenseAssignment { get; set; } = null!;
    }
}