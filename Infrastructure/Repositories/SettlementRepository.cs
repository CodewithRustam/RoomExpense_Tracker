namespace Infrastructure.Repositories
{
    public class SettlementRepository : Repository<Settlement>, ISettlementRepository
    {

        public SettlementRepository(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Retrieves settlements for a specific room and month using an index-friendly date range.
        /// </summary>
        public async Task<IReadOnlyList<Settlement>> GetMonthlySettlements(int roomId, DateTime selectedMonth)
        {
            var startOfMonth = new DateTime(selectedMonth.Year, selectedMonth.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            return await GetAllAsync(x => x.RoomId == roomId
                                       && x.SettlementForDate >= startOfMonth
                                       && x.SettlementForDate < endOfMonth);
        }

        /// <summary>
        /// Executes a stored procedure using Dapper by reusing EF Core's underlying connection.
        /// </summary>
        public async Task<IReadOnlyList<MonthlySettlementDto>> GetMonthlySettlementsDetails(int roomId, DateTime? targetMonth = null)
        {
            // Reuses the database connection owned by the DbContext context instance
            var connection = _context.Database.GetDbConnection();

            var parameters = new
            {
                RoomId = roomId,
                MonthDate = targetMonth ?? DateTime.Now
            };

            var result = await connection.QueryAsync<MonthlySettlementDto>(
                "GetMonthlyMemberBalances",
                param: parameters,
                commandType: System.Data.CommandType.StoredProcedure
            );

            return result as IReadOnlyList<MonthlySettlementDto> ?? result.ToList();
        }

        /// <summary>
        /// Query structured to allow the database optimizer to use indexes on RoomId and SettlementForDate cleanly.
        /// </summary>
        public async Task<IReadOnlyList<Settlement>> GetSettlementsForMembers(int roomId, int payerMemberId, int receiverMemberId, DateTime monthStart, DateTime monthEnd)
        {
            return await _context.Settlements
                .AsNoTracking()
                .Where(s => s.RoomId == roomId
                            && (s.MemberId == payerMemberId || s.MemberId == receiverMemberId || s.PaidToMemberId == payerMemberId || s.PaidToMemberId == receiverMemberId)
                            && s.SettlementForDate >= monthStart
                            && s.SettlementForDate <= monthEnd)
                .ToListAsync();
        }

        /// <summary>
        /// Optimized check to see if a month is settled without pulling back any entity data.
        /// </summary>
        public async Task<bool> IsMonthSettledForRoomAsync(int roomId, DateTime month)
        {
            var startOfMonth = new DateTime(month.Year, month.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            return await _context.Settlements.AsNoTracking().AnyAsync(s => s.RoomId == roomId 
                                                                        && s.SettlementForDate >= startOfMonth
                                                                        && s.SettlementForDate < endOfMonth);
        }
    }
}