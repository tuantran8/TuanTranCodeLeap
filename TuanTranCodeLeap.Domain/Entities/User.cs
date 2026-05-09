using Microsoft.AspNetCore.Identity;

namespace TuanTranCodeLeap.Domain.Entities;

public class User : IdentityUser<int>
{
    public int? CompanyId { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public string? FirstName { get; set; }
    public bool IsActive { get; set; } = true;
    public string? LastName { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool Deleted { get; set; } = false;
    public Guid Guid { get; set; } = Guid.NewGuid();
    
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
