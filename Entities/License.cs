namespace SaasLicenseSystem.Api.Entities
{
    public enum LicenseType { Trial, Monthly, Yearly, Enterprise }
    public enum LicenseStatus { Active, Expired, Suspended, Revoked }

    public class License : BaseEntity
    {
        public required string Name {get; set;}
        public required string LicenseKey { get; set; }
        public LicenseType Type { get; set; }
        public LicenseStatus Status { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        
        public int MaxSeats { get; set; } 
        
        public ICollection<LicenseAssignment> Assignments { get; set; } = new List<LicenseAssignment>();
    }
}