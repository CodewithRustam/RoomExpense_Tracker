namespace Services.Interfaces
{
    /// <summary>
    /// Service for loading, caching, and rendering dynamic email templates from the database.
    /// Implemented as a thread-safe Singleton with lazy initialization.
    /// </summary>
    public interface IEmailTemplateService
    {
        /// <summary>
        /// Renders the HTML body of an email template using the provided dynamic placeholders.
        /// </summary>
        Task<string> GetRenderedTemplateAsync(string templateKey, IDictionary<string, string> placeholders);

        /// <summary>
        /// Renders the subject line of an email template using the provided dynamic placeholders.
        /// </summary>
        Task<string> GetRenderedSubjectAsync(string templateKey, IDictionary<string, string> placeholders);

        /// <summary>
        /// Renders both subject and HTML body for an email template.
        /// </summary>
        Task<(string Subject, string Body)> RenderEmailAsync(string templateKey, IDictionary<string, string> placeholders);

        /// <summary>
        /// Helper for rendering Password Reset email.
        /// </summary>
        Task<string> GetPasswordResetEmailAsync(string resetLink, string memberName);

        /// <summary>
        /// Helper for rendering Settlement Notification email.
        /// </summary>
        Task<(string Subject, string Body)> GetSettlementEmailAsync(string userName, string mainMessage, string heading, int roomId, string? settlementMonth = null);

        /// <summary>
        /// Helper for rendering Room Invitation email.
        /// </summary>
        Task<(string Subject, string Body)> GetRoomInvitationEmailAsync(string inviterName, string roomName, string joinUrl, string inviteeName);

        /// <summary>
        /// Invalidates the in-memory cache, causing the templates to be lazily reloaded from the database on next access.
        /// </summary>
        void InvalidateCache();
    }
}
