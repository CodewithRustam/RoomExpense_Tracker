namespace Domain.Entities
{
    /// <summary>
    /// Represents an email template stored in the database.
    /// Loaded on demand via singleton lazy caching for optimal performance.
    /// </summary>
    public class EmailTemplate : BaseAuditableEntity
    {
        public int EmailTemplateId { get; set; }

        /// <summary>
        /// Unique identifier key for the template (e.g. PasswordReset, SettlementNotification, RoomInvitation).
        /// </summary>
        public string TemplateKey { get; set; } = string.Empty;

        /// <summary>
        /// The subject line template supporting dynamic placeholders (e.g. {{RoomName}}).
        /// </summary>
        public string SubjectTemplate { get; set; } = string.Empty;

        /// <summary>
        /// The full HTML body containing dynamic placeholders (e.g. {{ResetLink}}, {{MemberName}}).
        /// </summary>
        public string BodyHtml { get; set; } = string.Empty;

        /// <summary>
        /// Optional human-readable description of what this email template is used for.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Whether the template is currently active.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
