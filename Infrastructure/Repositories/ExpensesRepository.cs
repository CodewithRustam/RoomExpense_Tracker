using Domain.Entities;
using Domain.Interfaces;
using ExpenseTrakcerHepler;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Repositories
{
    public class ExpensesRepository : Repository<Expense>, IExpenseRepository
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache cache;

        public ExpensesRepository(AppDbContext context, IMemoryCache _cache) : base(context)
        {
            _context = context;
            cache = _cache;
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

        public async Task<List<Expense>> GetUserExpenses(string userId, DateTime startDate, DateTime endDate)
        {
            return await _context.Expenses
                .Include(e => e.Member)
                .Include(e=>e.Room)
                .Where(e => e.Member.ApplicationUserId == userId && e.Date >= startDate && e.Date <= endDate && (e.IsDeleted == false || e.IsDeleted == null)).OrderByDescending(x=>x.Date)
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
    }
}
