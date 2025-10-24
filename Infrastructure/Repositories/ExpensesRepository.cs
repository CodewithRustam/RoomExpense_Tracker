namespace Infrastructure.Repositories
{
    public class ExpensesRepository : Repository<Expense>, IExpenseRepository
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache cache;
        private readonly string _connectionString;

        public ExpensesRepository(AppDbContext context, IMemoryCache _cache, IConfiguration configuration) : base(context)
        {
            _context = context;
            cache = _cache;
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        }

        public async Task<string> AddExpenses(Expense expense)
        {
            string message = string.Empty;
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await AddAsync(expense);
                await SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync();
                return message = "Expense could not be added due to database constraints. Please check your input.";
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                return message = "An unexpected error occurred while adding the expense. Please try again.";
            }
            message = "Expense has been recorded successfully.";
            return message;
        }
        public async Task<List<ExpenseRecordDto>> GetMonthlyExpenses(int roomId, DateTime selectedMonth)
        {
            int year = selectedMonth.Year;
            int month = selectedMonth.Month;

            return await (from exp in _context.Expenses
                          join mem in _context.Members
                          on exp.MemberId equals mem.MemberId
                          where exp.RoomId == roomId && (exp.IsDeleted == false || exp.IsDeleted == null) && exp.Date.Month == month && exp.Date.Year == year
                          select new ExpenseRecordDto
                          {
                              ApplicationUserId = mem.ApplicationUserId ?? string.Empty,
                              PayerName = mem.Name,
                              PayerId = mem.MemberId,
                              ExpenseId = exp.ExpenseId,
                              RoomId = exp.RoomId,
                              Item = exp.Item ?? string.Empty,
                              Amount = exp.Amount,
                              Date = exp.Date,
                              Category = exp.Category ?? string.Empty,
                          }).OrderByDescending(x => x.ExpenseId).ToListAsync();
        }

        public async Task<bool> IsExpenseExist(Expense expense)
        {
            return await AnyAsync(e => e.RoomId == expense.RoomId &&
                                       e.MemberId == expense.MemberId &&
                                       e.Date == expense.Date && e.Amount == expense.Amount);
        }

        public async Task<(bool IsUpdated, string Message)> UpdateExpenses(Expense expense)
        {
            var expenseData = await FirstOrDefaultAsync(x =>
                x.ExpenseId == expense.ExpenseId &&
                x.RoomId == expense.RoomId &&
                (x.IsDeleted == false || x.IsDeleted == null));

            if (expenseData is null)
                return (false, "Expense not found.");

            expenseData.Item = expense.Item?.Trim();
            expenseData.Amount = expense.Amount;
            expenseData.Date = expense.Date.Date;
            expenseData.RoomId = expense.RoomId;

            Update(expenseData);
            await SaveChangesAsync();

            return (true, "Expense has been updated successfully.");
        }
        public async Task<decimal> GetTotalRoomExpenses(int roomId, DateTime start, DateTime end)
        {
                return await _context.Expenses.Where(e => e.RoomId == roomId && 
                                                          (e.IsDeleted == false || e.IsDeleted == null) && 
                                                          e.Date >= start && e.Date <= end).SumAsync(e => e.Amount);
        }
        public async Task<List<string>> GetExpenseMonths(int roomId)
        {
            return await _context.Expenses
                .AsNoTracking()
                .Where(e => e.RoomId == roomId && e.IsDeleted == false || e.IsDeleted == null)
                .Select(e => new { e.Date.Year, e.Date.Month })
                .Distinct()
                .OrderByDescending(x => x.Year)
                .ThenByDescending(x => x.Month)
                .Select(x => $"{x.Year:D4}-{x.Month:D2}") 
                .ToListAsync();
        }

        public async Task<List<UserExpenseDto>> GetUserExpenses(string userId, DateTime month)
        {
            var startOfMonth = new DateTime(month.Year, month.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1);

            return await (from e in _context.Expenses.AsNoTracking()
                          join m in _context.Members.AsNoTracking() on e.MemberId equals m.MemberId
                          join r in _context.Rooms.AsNoTracking() on e.RoomId equals r.RoomId
                          where m.ApplicationUserId == userId
                                && e.Date >= startOfMonth && e.Date < endOfMonth
                                && (e.IsDeleted == false || e.IsDeleted == null)
                          orderby e.Date descending
                          select new UserExpenseDto
                          {
                              Item = e.Item ?? string.Empty,
                              Amount = e.Amount,
                              ExpenseDate = e.Date,
                              MemberName = m.Name,
                              RoomName = r.Name,
                              Category = e.Category
                          })
                         .ToListAsync();
        }

        public List<string?> GetDeviceToken(int roomId)
        {
           return _context.Members
                  .Where(m => m.RoomId == roomId && m.ApplicationUser!.DeviceToken != null)
                  .Select(m => m.ApplicationUser!.DeviceToken)
                  .ToList();
        }

        public async Task<List<Expense>> GetExpensesForMembers(int roomId, int payerMemberId, int receiverMemberId, DateTime monthStart, DateTime monthEnd)
        {
            var memberIds = new[] { payerMemberId, receiverMemberId };

            return await _context.Expenses
                .Where(e => e.RoomId == roomId
                            && memberIds.Contains(e.MemberId)
                            && (e.IsDeleted == false || e.IsDeleted == null)
                            && e.Date >= monthStart
                            && e.Date <= monthEnd)
                .ToListAsync();
        }
        public async Task<(List<MemberExpensesDto> Members, List<CategoryExpenseDto> Categories, List<CategoryExpenseDto> TopSpends)> GetMonthlyExpensesTrendAsync(int roomId, DateTime targetMonth)
        {
            string cacheKey = $"MonthlyExpensesTrend_{roomId}_{targetMonth:yyyyMM}";
            var currentMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            //Don't cache current month (it changes frequently)
            if (targetMonth != currentMonth && cache.TryGetValue(cacheKey, out (List<MemberExpensesDto>, List<CategoryExpenseDto>, List<CategoryExpenseDto>) cachedData))
            {
                return cachedData;
            }

            using var connection = new SqlConnection(_connectionString);

            using (var multi = await connection.QueryMultipleAsync(
                "GetMonthlyExpensesTrend",
                new { RoomId = roomId, MonthDate = targetMonth },
                commandType: CommandType.StoredProcedure))
            {
                var members = (await multi.ReadAsync<MemberExpensesDto>()).ToList();
                var categories = (await multi.ReadAsync<CategoryExpenseDto>()).ToList();
                var topSpends = (await multi.ReadAsync<CategoryExpenseDto>()).ToList();

                var result = (members, categories, topSpends);

                if (targetMonth != currentMonth)
                {
                    cache.Set(cacheKey, result, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12)
                    });
                }

                return result;
            }
        }

        public async Task<List<string>> GetExpenseMonthsByUserId(string userId)
        {
            // Get all MemberIds for this user
            var memberIds = await _context.Members
                .Where(m => m.ApplicationUserId == userId)
                .Select(m => m.MemberId)
                .ToListAsync();

            if (memberIds == null || memberIds.Count == 0)
                return new List<string>();

            // Fetch all distinct Year-Month combinations from Expenses for these members
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

    }
}
