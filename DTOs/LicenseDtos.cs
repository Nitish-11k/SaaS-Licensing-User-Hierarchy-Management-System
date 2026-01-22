using SaasLicenseSystem.Api.Entities;

namespace SaasLicenseSystem.Api.DTOs
{
    public record CreateLicenseRequest(string Name, LicenseType Type, int MaxSeats, int DurationInDays);
    
    public record AssignLicenseRequest(Guid LicenseId, Guid TargetUserId);

    public record MachineHeartbeatRequest(
        string HardwareId, 
        string MachineName, 
        string OperatingSystem, 
        string IpAddress
    );

    public record MachineValidationResponse(bool IsAllowed, string Message);
}