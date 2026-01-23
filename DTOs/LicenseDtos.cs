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

    public record TransferLicenseRequest(Guid AssignmentId, Guid NewUserId);

    public record MachineDto(
    Guid Id, 
    string HardwareId, 
    string? MachineName, 
    string? OperatingSystem, 
    string? IpAddress, 
    DateTime LastActive,
    string AssignedUserEmail
    );

    public record LicenseUsageStats(int TotalLicenses, int ActiveLicenses, int TotalSeats, int AssignedSeats);
    public record UpgradeLicenseRequest(Guid LicenseId, int AddedSeats, int AddedDays);
}