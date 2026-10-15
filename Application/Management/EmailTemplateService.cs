using System.Text.RegularExpressions;

namespace Services.Management
{
    /// <summary>
    /// Thread-safe Singleton service that lazily loads and caches email templates from the database.
    /// Templates are fetched from SQL Server only upon first demand, and subsequent calls are served
    /// with O(1) in-memory performance. Supports dynamic placeholder replacement and graceful fallbacks.
    /// </summary>
    public class EmailTemplateService : IEmailTemplateService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailTemplateService> _logger;

        // Lazy initializer ensuring single execution and publication across concurrent threads
        private Lazy<Task<IReadOnlyDictionary<string, CachedEmailTemplate>>> _lazyTemplates;
        private readonly object _syncLock = new();

        public EmailTemplateService(
            IServiceScopeFactory scopeFactory,
            ILogger<EmailTemplateService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _lazyTemplates = CreateLazyInitializer();
        }

        private Lazy<Task<IReadOnlyDictionary<string, CachedEmailTemplate>>> CreateLazyInitializer()
        {
            return new Lazy<Task<IReadOnlyDictionary<string, CachedEmailTemplate>>>(
                () => LoadTemplatesFromDbAsync(),
                LazyThreadSafetyMode.ExecutionAndPublication
            );
        }

        /// <summary>
        /// Lazily queries the database for all active templates inside an isolated service scope.
        /// </summary>
        private async Task<IReadOnlyDictionary<string, CachedEmailTemplate>> LoadTemplatesFromDbAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var templates = await dbContext.EmailTemplates
                    .AsNoTracking()
                    .Where(t => t.IsActive)
                    .ToListAsync();

                _logger.LogInformation("Successfully loaded {Count} email template(s) from database into lazy cache.", templates.Count);

                return templates.ToDictionary(
                    t => t.TemplateKey,
                    t => new CachedEmailTemplate(t.TemplateKey, t.SubjectTemplate, t.BodyHtml),
                    StringComparer.OrdinalIgnoreCase
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load email templates from database. In-memory fallbacks will be used.");
                return new Dictionary<string, CachedEmailTemplate>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public void InvalidateCache()
        {
            lock (_syncLock)
            {
                _lazyTemplates = CreateLazyInitializer();
                _logger.LogInformation("Email templates lazy cache invalidated. Fresh templates will be loaded on next request.");
            }
        }

        public async Task<string> GetRenderedTemplateAsync(string templateKey, IDictionary<string, string> placeholders)
        {
            var (_, body) = await RenderEmailAsync(templateKey, placeholders);
            return body;
        }

        public async Task<string> GetRenderedSubjectAsync(string templateKey, IDictionary<string, string> placeholders)
        {
            var (subject, _) = await RenderEmailAsync(templateKey, placeholders);
            return subject;
        }

        public async Task<(string Subject, string Body)> RenderEmailAsync(string templateKey, IDictionary<string, string> placeholders)
        {
            var templateDict = await _lazyTemplates.Value;

            if (templateDict.TryGetValue(templateKey, out var cachedTemplate))
            {
                string renderedSubject = ReplacePlaceholders(cachedTemplate.SubjectTemplate, placeholders);
                string renderedBody = ReplacePlaceholders(cachedTemplate.BodyHtml, placeholders);
                return (renderedSubject, renderedBody);
            }

            _logger.LogWarning("Email template with key '{Key}' not found in active database cache. Falling back to built-in template.", templateKey);
            return GetFallbackEmail(templateKey, placeholders);
        }

        public async Task<string> GetPasswordResetEmailAsync(string resetLink, string memberName)
        {
            var placeholders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "ResetLink", resetLink },
                { "MemberName", memberName }
            };

            return await GetRenderedTemplateAsync(EmailTemplateKeys.PasswordReset, placeholders);
        }

        public async Task<(string Subject, string Body)> GetSettlementEmailAsync(
            string userName, 
            string mainMessage, 
            string heading, 
            int roomId, 
            string? settlementMonth = null)
        {
            var placeholders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "UserName", userName },
                { "MainMessage", mainMessage },
                { "Heading", heading },
                { "RoomId", roomId.ToString() },
                { "SettlementMonth", settlementMonth ?? string.Empty }
            };

            return await RenderEmailAsync(EmailTemplateKeys.SettlementNotification, placeholders);
        }

        public async Task<(string Subject, string Body)> GetRoomInvitationEmailAsync(
            string inviterName, 
            string roomName, 
            string joinUrl, 
            string inviteeName)
        {
            var placeholders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "InviterName", inviterName },
                { "RoomName", roomName },
                { "JoinUrl", joinUrl },
                { "InviteeName", inviteeName }
            };

            return await RenderEmailAsync(EmailTemplateKeys.RoomInvitation, placeholders);
        }

        /// <summary>
        /// Replaces both {{Key}} and {Key} tokens in the given text case-insensitively.
        /// </summary>
        private static string ReplacePlaceholders(string template, IDictionary<string, string> placeholders)
        {
            if (string.IsNullOrEmpty(template) || placeholders == null || placeholders.Count == 0)
                return template;

            string result = template;
            foreach (var kvp in placeholders)
            {
                // Match {{Key}} or {Key} case-insensitively
                string pattern = @"\{\{?" + Regex.Escape(kvp.Key) + @"\}\}?";
                result = Regex.Replace(result, pattern, kvp.Value ?? string.Empty, RegexOptions.IgnoreCase);
            }
            return result;
        }

        /// <summary>
        /// Safe in-memory fallback using EmailTemplates static helpers if database table record is missing.
        /// </summary>
        private static (string Subject, string Body) GetFallbackEmail(string templateKey, IDictionary<string, string> placeholders)
        {
            switch (templateKey)
            {
                case EmailTemplateKeys.PasswordReset:
                {
                    string resetLink = placeholders.TryGetValue("ResetLink", out var r) ? r : string.Empty;
                    string memberName = placeholders.TryGetValue("MemberName", out var m) ? m : "User";
                    return ("Reset Your Password", EmailTemplates.GetPasswordResetEmail(resetLink, memberName));
                }

                case EmailTemplateKeys.SettlementNotification:
                {
                    string userName = placeholders.TryGetValue("UserName", out var u) ? u : "User";
                    string mainMessage = placeholders.TryGetValue("MainMessage", out var msg) ? msg : string.Empty;
                    string heading = placeholders.TryGetValue("Heading", out var h) ? h : "Settlement Notification";
                    int roomId = placeholders.TryGetValue("RoomId", out var rId) && int.TryParse(rId, out var id) ? id : 0;
                    string month = placeholders.TryGetValue("SettlementMonth", out var sm) ? sm : string.Empty;

                    string subject = string.IsNullOrWhiteSpace(month) ? heading : $"{heading} - {month}";
                    return (subject, EmailTemplates.GetSettlementEmailTemplate(userName, mainMessage, heading, roomId));
                }

                case EmailTemplateKeys.RoomInvitation:
                {
                    string inviterName = placeholders.TryGetValue("InviterName", out var iname) ? iname : "A friend";
                    string roomName = placeholders.TryGetValue("RoomName", out var rname) ? rname : "Room";
                    string joinUrl = placeholders.TryGetValue("JoinUrl", out var url) ? url : string.Empty;
                    string inviteeName = placeholders.TryGetValue("InviteeName", out var ename) ? ename : "Friend";

                    return ($"You're invited to join '{roomName}' on SplitX", EmailTemplates.GetRoomInvitationEmail(inviterName, roomName, joinUrl, inviteeName));
                }

                default:
                    return ("Notification", string.Empty);
            }
        }

        private sealed record CachedEmailTemplate(string TemplateKey, string SubjectTemplate, string BodyHtml);
    }
}
