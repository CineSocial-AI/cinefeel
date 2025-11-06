namespace CineSocial.Domain.Common;

public abstract class BaseAuditableEntity : BaseEntity
{
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public Guid? DeletedBy { get; set; }
}
