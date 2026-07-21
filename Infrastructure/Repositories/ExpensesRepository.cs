namespace Infrastructure.Repositories
{
    public class ExpensesRepository : Repository<Expense>, IExpenseRepository
    {
        private readonly IMemoryCache _cache;

        public ExpensesRepository(AppDbContext context, IMemoryCache cache) : base(context)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Optimized with a continuous range query instead of .Month/.Year to enable index usage.
        /// </summary>
        public async Task<IReadOnlyList<ExpenseRecordDto>> GetMonthlyExpenses(int roomId, DateTime selectedMonth)
        {
            var startOfMonth = new DateTime(selectedMonth.Year, selectedMonth.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            return await (from exp in _context.Expenses.AsNoTracking()
                          join mem in _context.Members.AsNoTracking() on exp.MemberId equals mem.MemberId
                          where exp.RoomId == roomId
                                && (exp.IsDeleted == false || exp.IsDeleted == null)
                                && exp.Date >= startOfMonth && exp.Date < endOfMonth
                                && !mem.IsDeleted
                          orderby exp.ExpenseId descending
                          select new ExpenseRecordDto
                          {
                              ApplicationUserId = mem.ApplicationUserId ?? string.Empty,
                              PayerName = mem.Name!,
                              PayerId = mem.MemberId,
                              ExpenseId = exp.ExpenseId,
                              RoomId = exp.RoomId,
                              Item = exp.Item ?? string.Empty,
                              Amount = exp.Amount,
                              Date = exp.Date,
                              Category = exp.Category ?? string.Empty,
                          }).ToListAsync();
        }

        public async Task<IReadOnlyList<UserExpenseDto>> GetUserExpenses(string userId, DateTime month)
        {
            var startOfMonth = new DateTime(month.Year, month.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            return await (from e in _context.Expenses.AsNoTracking()
                          join m in _context.Members.AsNoTracking() on e.MemberId equals m.MemberId
                          join r in _context.Rooms.AsNoTracking() on e.RoomId equals r.RoomId
                          where m.ApplicationUserId == userId
                                && e.Date >= startOfMonth && e.Date < endOfMonth
                                && (e.IsDeleted == false || e.IsDeleted == null)
                                && !m.IsDeleted
                                && !r.IsDeleted
                          orderby e.Date descending
                          select new UserExpenseDto
                          {
                              Item = e.Item ?? string.Empty,
                              Amount = e.Amount,
                              ExpenseDate = e.Date,
                              MemberName = m.Name!,
                              RoomName = r.Name!,
                              Category = e.Category
                          }).ToListAsync();
        }

        public async Task<IReadOnlyList<Expense>> GetExpensesForMembers(int roomId, int payerMemberId, int receiverMemberId, DateTime monthStart, DateTime monthEnd)
        {
            return await _context.Expenses
                .AsNoTracking()
                .Where(e => e.RoomId == roomId
                            && (e.MemberId == payerMemberId || e.MemberId == receiverMemberId)
                            && (e.IsDeleted == false || e.IsDeleted == null)
                            && e.Date >= monthStart
                            && e.Date <= monthEnd)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Expense>> GetHomeExpenseTrends(int roomId, DateTime startDate)
        {
            return await _context.Expenses
                .AsNoTracking()
                .Where(e => e.RoomId == roomId &&
                            (e.IsDeleted == false || e.IsDeleted == null) &&
                            e.Date >= startDate)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<string>> GetExpenseMonths(int roomId)
        {
            return await _context.Expenses
                .AsNoTracking()
                .Where(e => e.RoomId == roomId && (e.IsDeleted == false || e.IsDeleted == null))
                .Select(e => new { e.Date.Year, e.Date.Month })
                .Distinct()
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .Select(x => $"{x.Year:D4}-{x.Month:D2}")
                .ToListAsync();
        }

        public async Task<IReadOnlyList<string>> GetExpenseMonthsByUserId(string userId)
        {
            var memberIds = await _context.Members
                .AsNoTracking()
                .Where(m => m.ApplicationUserId == userId && !m.IsDeleted)
                .Select(m => m.MemberId)
                .ToListAsync();

            if (memberIds.Count == 0)
                return Array.Empty<string>();

            return await _context.Expenses
                .AsNoTracking()
                .Where(e => memberIds.Contains(e.MemberId) && (e.IsDeleted == false || e.IsDeleted == null))
                .Select(e => new { e.Date.Year, e.Date.Month })
                .Distinct()
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .Select(x => $"{x.Year:D4}-{x.Month:D2}")
                .ToListAsync();
        }

        /// <summary>
        /// Reuses the existing EF Db Connection for Dapper and handles clean mapping.
        /// </summary>
        public async Task<(IReadOnlyList<MemberExpensesDto> Members, IReadOnlyList<CategoryExpenseDto> Categories, IReadOnlyList<CategoryExpenseDto> TopSpends)> GetMonthlyExpensesTrendAsync(int roomId, DateTime targetMonth)
        {
            string cacheKey = $"MonthlyExpensesTrend_{roomId}_{targetMonth:yyyyMM}";
            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            if (targetMonth != currentMonth && _cache.TryGetValue(cacheKey, out (IReadOnlyList<MemberExpensesDto>, IReadOnlyList<CategoryExpenseDto>, IReadOnlyList<CategoryExpenseDto>) cachedData))
            {
                return cachedData;
            }

            var connection = _context.Database.GetDbConnection();

            using var multi = await connection.QueryMultipleAsync(
                "GetMonthlyExpensesTrend",
                new { RoomId = roomId, MonthDate = targetMonth },
                commandType: CommandType.StoredProcedure);

            var members = (await multi.ReadAsync<MemberExpensesDto>()).ToList();
            var categories = (await multi.ReadAsync<CategoryExpenseDto>()).ToList();
            var topSpends = (await multi.ReadAsync<CategoryExpenseDto>()).ToList();

            var result = (members, categories, topSpends);

            if (targetMonth != currentMonth)
            {
                _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12)
                });
            }

            return result;
        }

        public async Task<decimal> GetTotalRoomExpenses(int roomId, DateTime start, DateTime end)
        {
            return await _context.Expenses
                .Where(e => e.RoomId == roomId &&
                            (e.IsDeleted == false || e.IsDeleted == null) &&
                            e.Date >= start && e.Date <= end)
                .SumAsync(e => e.Amount);
        }

        public Task<bool> IsExpenseExist(Expense expense)
        {
            return AnyAsync(e => e.RoomId == expense.RoomId &&
                                 e.MemberId == expense.MemberId &&
                                 e.Item == expense.Item &&
                                 e.Date == expense.Date &&
                                 e.Amount == expense.Amount);
        }

        public Task<bool> IsExpenseExistForUser(Expense expense)
        {
            return AnyAsync(x => x.ExpenseId == expense.ExpenseId &&
                                 x.RoomId == expense.RoomId &&
                                 (x.IsDeleted == false || x.IsDeleted == null));
        }
    }
}