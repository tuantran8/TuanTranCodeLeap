using Microsoft.AspNetCore.Identity;

namespace TuanTranCodeLeap.Domain.Entities;

public class UserRole : IdentityUserRole<int>
{
    public int? CompanyId { get; set; }
    public bool IsOwner { get; set; }
    
    public virtual User? User { get; set; }
    public virtual Role? Role { get; set; }
}
