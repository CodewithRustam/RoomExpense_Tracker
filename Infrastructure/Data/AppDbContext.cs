using ExpenseTrakcerHepler;

namespace Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
    {
        private readonly ICurrentUserService? _currentUserService;

        public AppDbContext(
            DbContextOptions<AppDbContext> options,
            ICurrentUserService? currentUserService = null) : base(options)
        {
            _currentUserService = currentUserService;
        }

        public DbSet<Room> Rooms { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<ExpenseSplit> ExpenseSplits { get; set; }
        public DbSet<DailyReportLog> DailyReportLogs { get; set; }
        public DbSet<Settlement> Settlements { get; set; }
        public DbSet<PasswordResetLink> PasswordResetLink { get; set; }
        public DbSet<PushNotification> Notifications { get; set; }
        public DbSet<RoomInvitation> RoomInvitations { get; set; }
        public DbSet<EmailTemplate> EmailTemplates { get; set; }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyAuditInformation();
            return await base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            ApplyAuditInformation();
            return base.SaveChanges();
        }

        private void ApplyAuditInformation()
        {
            var currentUserId = _currentUserService?.UserId;
            var now = DateTimeProvider.NowIST;

            foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Entity.CreatedDate == default)
                    {
                        entry.Entity.CreatedDate = now;
                    }
                    if (string.IsNullOrEmpty(entry.Entity.CreatedByUserId) && !string.IsNullOrEmpty(currentUserId))
                    {
                        entry.Entity.CreatedByUserId = currentUserId;
                    }
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Entity.UpdatedDate = now;
                    if (!string.IsNullOrEmpty(currentUserId))
                    {
                        entry.Entity.UpdatedByUserId = currentUserId;
                    }
                }
            }
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<MonthlySettlementDto>().HasNoKey();

            modelBuilder.Entity<MonthlySettlementDto>()
                        .Property(p => p.NetBalance)
                        .HasPrecision(18, 2); 

            modelBuilder.Entity<Expense>(entity =>
            {
                entity.HasKey(e => e.ExpenseId);

                entity.HasOne(e => e.Member)
                      .WithMany()
                      .HasForeignKey(e => e.MemberId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Room)
                      .WithMany(r => r.Expenses)
                      .HasForeignKey(e => e.RoomId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Amount)
                      .HasPrecision(18, 2);

                entity.HasIndex(e => e.RoomId);
                entity.HasIndex(e => new { e.RoomId, e.Date });
            });

            modelBuilder.Entity<Member>(entity =>
            {
                entity.HasOne(m => m.Room)
                    .WithMany(r => r.Members)
                    .HasForeignKey(m => m.RoomId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(m => m.JoinedDate)
                    .HasDefaultValueSql("DATEADD(MINUTE, 330, GETUTCDATE())");
            });

            modelBuilder.Entity<Room>(entity =>
            {
                entity.HasOne(r => r.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(r => r.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(r => r.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<RoomInvitation>(entity =>
            {
                entity.HasKey(i => i.Id);

                entity.HasOne(i => i.Room)
                      .WithMany(r => r.Invitations)
                      .HasForeignKey(i => i.RoomId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(i => i.InvitedEmail)
                      .HasMaxLength(256)
                      .IsRequired();

                entity.Property(i => i.InvitedName)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.Property(i => i.Token)
                      .HasMaxLength(128)
                      .IsRequired();

                entity.HasIndex(i => i.Token)
                      .IsUnique();

                entity.HasIndex(i => new { i.RoomId, i.InvitedEmail, i.Status });
            });

            modelBuilder.Entity<ExpenseSplit>(entity =>
            {
                entity.HasKey(es => es.ExpenseSplitId);

                entity.HasOne(es => es.Expense)
                      .WithMany(e => e.ExpenseSplits)
                      .HasForeignKey(es => es.ExpenseId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(es => es.Member)
                      .WithMany()
                      .HasForeignKey(es => es.MemberId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.Property(es => es.OwedAmount)
                      .HasPrecision(18, 2);

                entity.Property(es => es.Percentage)
                      .HasPrecision(18, 2);

                entity.HasIndex(es => es.ExpenseId);
                entity.HasIndex(es => new { es.ExpenseId, es.MemberId })
                      .IsUnique();
            });

            modelBuilder.Entity<Settlement>()
                   .Property(s => s.Amount)
                   .HasPrecision(18, 2); 

            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.HasKey(e => e.EmailTemplateId);

                entity.Property(e => e.TemplateKey)
                      .HasMaxLength(100)
                      .IsRequired();

                entity.HasIndex(e => e.TemplateKey)
                      .IsUnique();

                entity.Property(e => e.SubjectTemplate)
                      .HasMaxLength(500)
                      .IsRequired();

                entity.Property(e => e.BodyHtml)
                      .IsRequired();

                entity.Property(e => e.Description)
                      .HasMaxLength(500);

                entity.Property(e => e.IsActive)
                      .HasDefaultValue(true);

                entity.Property(e => e.CreatedDate)
                      .HasDefaultValueSql("DATEADD(MINUTE, 330, GETUTCDATE())");
            });
        }
    }
}
