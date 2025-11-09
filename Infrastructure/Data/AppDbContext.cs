namespace Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Room> Rooms { get; set; }
        public DbSet<Member> Members { get; set; }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<DailyReportLog> DailyReportLogs { get; set; }
        public DbSet<Settlement> Settlements { get; set; }
        public DbSet<PasswordResetLink> PasswordResetLink { get; set; }
        public DbSet<PushNotification> Notifications { get; set; }

        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<MonthlySettlementDto>().HasNoKey();

            modelBuilder.Entity<Expense>(entity =>
            {
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
            });

            modelBuilder.Entity<Member>()
                .HasOne(m => m.Room)
                .WithMany(r => r.Members)
                .HasForeignKey(m => m.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Room>()
                .HasOne(r => r.CreatedByUser)
                .WithMany()
                .HasForeignKey(r => r.CreatedByUserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Settlement>()
                   .Property(s => s.Amount)
                   .HasPrecision(18, 2); 
        }
    }
}
