using Domain.Entities;
using Domain.Interfaces;
using ExpenseTrakcerHepler;
using Microsoft.Extensions.Caching.Memory;
using Services.Interfaces;
using Services.ViewModels;
using Services.ViewModels.ApiViewModels;

namespace Services.Management
{
    public class ExpenseService : IExpenseServices
    {
		private readonly IExpenseRepository expenseRepository;
		private readonly ISettlementRepository settlementRepository;
		private readonly IMemberRepository memberRepository;
        private readonly ICurrentUserService currentUser;
        private readonly IMemoryCache cache;
        public ExpenseService(IExpenseRepository _expenseRepository, ISettlementRepository _settlementRepository, IMemberRepository _memberRepository, ICurrentUserService _currentUser, IMemoryCache _cache) 
		{
            expenseRepository = _expenseRepository;
            settlementRepository = _settlementRepository;
            memberRepository = _memberRepository;
            currentUser = _currentUser;
            cache = _cache;
        }
        public async Task<string> AddExpenses(ExpenseViewModel viewModel)
        {
            string message = string.Empty;
			try
			{
                var error = ValidateExpenseViewModel(viewModel);

                if (!string.IsNullOrEmpty(error))
                {
                    return message = error;
                }
                else
                {
                    var userId = currentUser.UserId;
                    var rateLimitKey = $"AddExpense-{userId}";

                    if (cache.TryGetValue(rateLimitKey, out int count))
                    {
                        if (count >= 3)
                        {
                            return message = "You are submitting too many expenses at once. Please wait a minute.";
                        }
                        cache.Set(rateLimitKey, count + 1, TimeSpan.FromMinutes(1));
                    }
                    else
                    {
                        cache.Set(rateLimitKey, 1, TimeSpan.FromMinutes(1));
                    }

                    if (viewModel is not null && viewModel.Expense is not null)
                    {
                        Expense expense = new Expense
                        {
                            MemberId = viewModel.Expense.MemberId,
                            Amount = viewModel.Expense.Amount,
                            RoomId = viewModel.Expense.RoomId,
                            IsNonSplitExpense = viewModel.Expense.IsNonSplitExpense,
                            Item = viewModel.Expense.Item,
                            Date = viewModel.Expense.Date,
                        };

                        bool exists = await expenseRepository.IsExpenseExist(expense);

                        if (exists)
                        {
                            return message = "This expense already exists.";
                        }

                        message = await expenseRepository.AddExpenses(expense);
                        if (expense is not null && expense.ExpenseId > 0)
                        {
                            var cacheKey = CacheHepler.GetCacheKey(viewModel.RoomId, viewModel.Expense.Date);
                            cache.Remove(cacheKey);
                        }
                    }
                    else
                    {
                        message = "Expense data is missing.";
                    }
                }
            }
			catch (Exception)
			{
				throw;
			}
            return message;
        }
        public async Task<string> UpdateExpenses(ExpenseViewModel viewModel)
        {
            try
            {
                var error = ValidateExpenseViewModel(viewModel);

                if (!string.IsNullOrEmpty(error))
                {
                    return error;
                }
                else
                {
                    if (viewModel is not null && viewModel.Expense is not null)
                    {
                        Expense expense = new Expense
                        {
                            ExpenseId = viewModel.Expense.ExpenseId,
                            Item = viewModel.Expense.Item?.Trim(),
                            Amount = viewModel.Expense.Amount,
                            Date = viewModel.Expense.Date.Date,
                            RoomId = viewModel.RoomId,
                            IsNonSplitExpense = viewModel.Expense.IsNonSplitExpense
                        };
                        var result = await expenseRepository.UpdateExpenses(expense);

                        if (result.IsUpdated)
                        {
                            var cacheKey = CacheHepler.GetCacheKey(viewModel.RoomId, viewModel.Expense.Date);
                            cache.Remove(cacheKey);
                        }
                        return result.Message;
                    }
                }            
            }
            catch (Exception)
            {
                throw;
            }
            return "Please enter input.";
        }

        public async Task<RoomExpensesViewModel> GetMonthlyExpenses(int roomId, DateTime selectedMonth)
        {
            RoomExpensesViewModel roomExpensesViewModel = new RoomExpensesViewModel();
            try
            {
                string cacheKey = CacheHepler.GetCacheKey(roomId, selectedMonth);

                if (!cache.TryGetValue(cacheKey, out MonthlyExpensesDataCacheVM? cachedData))
                {
                    cachedData = new MonthlyExpensesDataCacheVM
                    {
                        Expenses = await expenseRepository.GetMonthlyExpenses(roomId, selectedMonth),
                        Settlements = await settlementRepository.GetMonthlySettlements(roomId, selectedMonth)
                    };

                    cache.Set(cacheKey, cachedData, TimeSpan.FromDays(30));
                }

                if (cachedData is not null)
                {
                    List<Expense>? expenses = cachedData.Expenses;
                    List<Settlement>? settlements = cachedData.Settlements;

                    var userId = currentUser.UserId;
                    List<Member> members = await memberRepository.GetMembersByRoomId(roomId,userId);

                    var total = expenses.Where(x => !x.IsNonSplitExpense).Sum(x => (decimal?)x.Amount) ?? 0m;
                    var memberCount = members.Count;
                    var avgAmount = memberCount > 0 ? Math.Round(total / memberCount, 2) : 0m;

                    List<ExpenseSummary> summaries = GetExpenseSummary(expenses, settlements, members);

                    roomExpensesViewModel = new RoomExpensesViewModel
                    {
                        Summary = summaries,
                        TotalExpense = total,
                        AvgPerPerson = avgAmount,
                        IsTwoMembersInRoom = members.Count == 2,
                        Expense = new Expense { RoomId = roomId }
                    };
                }
            }
            catch (Exception)
            {
                throw;
            }
            return roomExpensesViewModel;
        }

        public string ValidateExpenseViewModel(ExpenseViewModel viewModel)
        {
            return viewModel switch
            {
                null => "Expense data is missing.",
                { Expense: null } => "Expense details are required.",
                { Expense.Item: null or "" } => "Expense item name is required.",
                { Expense.Amount: <= 0 } => "Expense amount must be greater than zero.",
                { RoomId: <= 0 } => "Room ID is invalid.",
                _ => string.Empty
            };
        }
        private List<ExpenseSummary> GetExpenseSummary(List<Expense> expenses, List<Settlement> settlements, List<Member> members)
        {
            List<ExpenseSummary> summaries = new List<ExpenseSummary>();
            try
            {
                var userId = currentUser.UserId;
                var total = expenses.Where(x => !x.IsNonSplitExpense).Sum(x => (decimal?)x.Amount) ?? 0m;
                var memberCount = members.Count;
                var avgAmount = memberCount > 0 ? Math.Round(total / memberCount, 2) : 0m;

                foreach (var member in members)
                {
                    var memberExpenses = expenses.Where(e => e.MemberId == member.MemberId && !e.IsNonSplitExpense).OrderBy(e => e.Date).ToList();
                    var totalExpense = memberExpenses.Sum(e => e.Amount);
                    var totalPaid = settlements.Where(s => s.MemberId == member.MemberId).Sum(s => s.Amount);
                    var totalReceived = settlements.Where(s => s.PaidToMemberId == member.MemberId).Sum(s => s.Amount);
                    var rawDifference = (totalExpense + totalPaid) - totalReceived - avgAmount;
                    var effectiveDifference = Math.Abs(rawDifference) < 0.5m ? 0m : rawDifference;

                    var tookFromOthers = expenses
                        .Where(e => e.MemberId == member.MemberId && e.OwedToMemberId != member.MemberId && e.OwedToMemberId > 0 && e.IsNonSplitExpense == true)
                        .Sum(e => e.Amount);

                    var gaveToOthers = expenses
                        .Where(e => e.MemberId == member.MemberId && e.OweToMemberId != member.MemberId && e.OweToMemberId > 0 && e.IsNonSplitExpense == true)
                        .Sum(e => e.Amount);

                    List<string> notes = new List<string>();

                    var loggedInMemberId = members.Where(x => x.ApplicationUserId == userId).Select(x => x.MemberId).FirstOrDefault();

                    string displayName = member.MemberId == loggedInMemberId ? "You" : member.Name;

                    if (gaveToOthers > 0)
                    {
                        int? memid = expenses.Where(e => e.MemberId == member.MemberId && e.OweToMemberId > 0 && e.IsNonSplitExpense).Select(x => x.OweToMemberId).FirstOrDefault();

                        string? oweMemberName = expenses.Where(e => e.MemberId == memid).Select(x => x.Member.Name).FirstOrDefault();
                        notes.Add($"{displayName} gave: {gaveToOthers:F2} to {oweMemberName}");
                    }

                    if (tookFromOthers > 0)
                    {
                        int? memid = expenses.Where(e => e.MemberId == member.MemberId && e.OwedToMemberId > 0 && e.IsNonSplitExpense).Select(x => x.OwedToMemberId).FirstOrDefault();

                        string? owedToMemberName = expenses.Where(e => e.MemberId == memid).Select(x => x.Member.Name).FirstOrDefault();
                        notes.Add($"{displayName} took: {tookFromOthers:F2} from {owedToMemberName}");
                    }

                    string personalNote = notes.Any() ? string.Join(" | ", notes) : "No non-split transactions";

                    summaries.Add(new ExpenseSummary
                    {
                        MemberName = member.Name,
                        TotalExpense = totalExpense,
                        PaidAmount = totalPaid,
                        ReceivedAmount = totalReceived,
                        NetBalance = totalExpense + totalPaid - totalReceived,
                        Items = memberExpenses,
                        IsOwed = effectiveDifference > 0,
                        IsOwing = effectiveDifference < 0,
                        BadgeText = effectiveDifference > 0 ? "Owed" : effectiveDifference < 0 ? "Owe" : "Settled up",
                        BadgeAmount = effectiveDifference != 0 ? Math.Abs(effectiveDifference) : 0m,
                        RawDifference = effectiveDifference,
                        NonSplitText = personalNote,
                    });
                }
            }
            catch (Exception)
            {

                throw;
            }
            return summaries;
        }
    }
}
