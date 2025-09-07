using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ExpensesRepository : Repository<Expense>, IExpenseRepository
    {
        private readonly AppDbContext _context;

        public ExpensesRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }

        public async Task<string> AddExpenses(Expense expense)
        {
            string message = string.Empty;
            try
            {
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
            }
            catch (Exception)   
            {
                throw;
            }
            return message;
        }
        public async Task<List<Expense>> GetMonthlyExpenses(int roomId, DateTime selectedMonth)
        {
            try
            {
                return await GetAllAsync(x => x.RoomId == roomId && (x.IsDeleted == false || x.IsDeleted == null)
                                                                 && x.Date.Year == selectedMonth.Year
                                                                 && x.Date.Month == selectedMonth.Month);

            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> IsExpenseExist(Expense expense)
        {
            try
            {
                return await AnyAsync(e => e.RoomId == expense.RoomId && e.MemberId == expense.MemberId &&
                                    e.Date == expense.Date && e.Amount == expense.Amount);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<(bool IsUpdated, string Message)> UpdateExpenses(Expense expense)
        {
            try
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
            catch (Exception)
            {
                return (false, "An unexpected error occurred while updating the expense. Please try again.");
            }
        }

        public async Task<decimal> GetMemberTotalExpenses(int roomId, int memberId, DateTime start, DateTime end)
        {
            try
            {
                return await _context.Expenses.Where(e => e.RoomId == roomId && e.MemberId == memberId && (e.IsDeleted == false || e.IsDeleted == null)
                                                   && e.Date >= start && e.Date <= end).SumAsync(e => e.Amount);
            }
            catch (Exception)
            {
                throw;
            }
        }
        public async Task<decimal> GetTotalRoomExpenses(int roomId, DateTime start, DateTime end)
        {
            try
            {
                return await _context.Expenses.Where(e => e.RoomId == roomId && (e.IsDeleted == false || e.IsDeleted == null) && e.Date >= start && e.Date <= end).SumAsync(e => e.Amount);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public List<string> GetAllMonthsWithExpenses(int roomId)
        {
            try
            {
                return _context.Expenses
                           .Where(e => e.RoomId == roomId).AsEnumerable()
                           .Select(e => e.Date.ToString("yyyy-MM")) // extract year-month string
                           .Distinct()
                           .OrderByDescending(m => m)
                           .ToList();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
