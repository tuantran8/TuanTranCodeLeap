using Microsoft.AspNetCore.Identity;

namespace TuanTranCodeLeap.Domain.Entities;

public class Role : IdentityRole<int>
{
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
