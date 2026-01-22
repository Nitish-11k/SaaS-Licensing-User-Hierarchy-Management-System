namespace SaasLicenseSystem.Api.Entities
{
    public class Group : BaseEntity
    {
        public required string Name { get; set; }
        public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    }

    public class UserGroup
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        
        public Guid GroupId { get; set; }
        public Group Group { get; set; } = null!;
    }
}