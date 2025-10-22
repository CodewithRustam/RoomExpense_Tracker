using Domain.Entities;
using Domain.Interfaces;
using ExpenseTrakcerHepler;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using Microsoft.Extensions.Configuration;
using System.Data.Common;
using Domain.Entities.Dto;

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
            message = "Expense added successfully";
            return message;
        }
        public async Task<List<Expense>> GetMonthlyExpenses(int roomId, DateTime selectedMonth)
        {
            var itemsToUpdate = await GetAllAsync(x => x.IsDeleted == false || x.IsDeleted == null);

            foreach (var item in itemsToUpdate)
            {
                item.Category = CategoryMapper.GetCategoryFromItem(item.Item!);
            }
            await SaveChangesAsync();
            string cacheKey = CacheHepler.GetCacheKey(roomId, selectedMonth);

            if (!cache.TryGetValue(cacheKey, out List<Expense>? expenseDataList))
            {
                expenseDataList = await GetAllAsync(x => x.RoomId == roomId && (x.IsDeleted == false || x.IsDeleted == null));
                cache.Set(cacheKey, expenseDataList, TimeSpan.FromDays(30));
            }
            return expenseDataList ?? new List<Expense>();
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
                return (false, "Expense not found");

            expenseData.Item = expense.Item?.Trim();
            expenseData.Amount = expense.Amount;
            expenseData.Date = expense.Date.Date;
            expenseData.RoomId = expense.RoomId;

            Update(expenseData);
            await SaveChangesAsync();

            return (true, "Expense updated successfully");
        }
        public async Task<decimal> GetTotalRoomExpenses(int roomId, DateTime start, DateTime end)
        {
                return await _context.Expenses.Where(e => e.RoomId == roomId && 
                                                          (e.IsDeleted == false || e.IsDeleted == null) && 
                                                          e.Date >= start && e.Date <= end).SumAsync(e => e.Amount);
        }
        public async Task<List<string>> GetExpenseMonths()
        {
            return await _context.Expenses
                .AsNoTracking()
                .Where(e => e.IsDeleted == false || e.IsDeleted == null)
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

    }
}
