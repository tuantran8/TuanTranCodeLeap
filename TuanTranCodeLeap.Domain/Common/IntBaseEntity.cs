namespace TuanTranCodeLeap.Domain.Common;

public abstract class IntBaseEntity
{
    public int Id { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool Deleted { get; set; } = false;
    public Guid Guid { get; set; } = Guid.NewGuid();
}
