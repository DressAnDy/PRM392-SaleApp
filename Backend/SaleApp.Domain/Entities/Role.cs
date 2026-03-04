namespace SaleApp.Domain.Entities;

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
