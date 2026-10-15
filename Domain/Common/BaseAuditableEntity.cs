using ExpenseTrakcerHepler;

namespace Domain.Common
{
    /// <summary>
    /// Base class providing standardized audit columns for entities.
    /// Values are automatically stamped by AppDbContext.SaveChangesAsync() in IST.
    /// </summary>
    public abstract class BaseAuditableEntity : IAuditableEntity
    {
        public DateTime CreatedDate { get; set; } = DateTimeProvider.NowIST;
        public string? CreatedByUserId { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? UpdatedByUserId { get; set; }
    }
}
