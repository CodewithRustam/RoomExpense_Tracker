namespace Domain.Common
{
    /// <summary>
    /// Standard audit contract for domain entities.
    /// Automatically populated by AppDbContext.SaveChangesAsync() via EF Core ChangeTracker.
    /// </summary>
    public interface IAuditableEntity
    {
        DateTime CreatedDate { get; set; }
        string? CreatedByUserId { get; set; }
        DateTime? UpdatedDate { get; set; }
        string? UpdatedByUserId { get; set; }
    }
}
