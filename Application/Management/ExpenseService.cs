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
		private readonly IRoomRepository roomRepository;
		private readonly IMemberRepository memberRepository;
        private readonly ICurrentUserService currentUser;
        private readonly IMemoryCache cache;
        public ExpenseService(IExpenseRepository _expenseRepository, ISettlementRepository _settlementRepository, IMemberRepository _memberRepository, IRoomRepository _roomRepository, ICurrentUserService _currentUser, IMemoryCache _cache) 
		{
            expenseRepository = _expenseRepository;
            settlementRepository = _settlementRepository;
            memberRepository = _memberRepository;
            roomRepository = _roomRepository;
            currentUser = _currentUser;
            cache = _cache;
        }
        public async Task<string> AddExpenses(ExpenseViewModel expenseViewModel)
        {
            string message = string.Empty;
            try
            {
                var error = ValidateExpenseViewModel(expenseViewModel);

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

                    if (expenseViewModel is not null)
                    {
                        Expense expense = new Expense
                        {
                            MemberId = expenseViewModel.MemberId,
                            Amount = expenseViewModel.Amount,
                            RoomId = expenseViewModel.RoomId,
                            Item = expenseViewModel.Item,
                            Date = expenseViewModel.Date,
                            Category = GetCategoryFromItem(expenseViewModel.Item ?? string.Empty)
                        };

                        bool exists = await expenseRepository.IsExpenseExist(expense);

                        if (exists)
                        {
                            return message = "This expense already exists.";
                        }

                        message = await expenseRepository.AddExpenses(expense);
                        if (expense is not null && expense.ExpenseId > 0)
                        {
                            var cacheKey = CacheHepler.GetCacheKey(expenseViewModel.RoomId, expenseViewModel.Date);
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
        public async Task<string> UpdateExpenses(ExpenseViewModel expenseViewModel)
        {
            try
            {
                var error = ValidateExpenseViewModel(expenseViewModel);

                if (!string.IsNullOrEmpty(error))
                {
                    return error;
                }
                else
                {
                    if (expenseViewModel is not null)
                    {
                        Expense expense = new Expense
                        {
                            ExpenseId = expenseViewModel.ExpenseId,
                            Item = expenseViewModel.Item?.Trim(),
                            Amount = expenseViewModel.Amount,
                            Date = expenseViewModel.Date.Date,
                            RoomId = expenseViewModel.RoomId,
                            Category = GetCategoryFromItem(expenseViewModel.Item ?? string.Empty)
                        };
                        var result = await expenseRepository.UpdateExpenses(expense);

                        if (result.IsUpdated)
                        {
                            var cacheKey = CacheHepler.GetCacheKey(expenseViewModel.RoomId, expenseViewModel.Date);
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
                { Item: null or "" } => "Expense item name is required.",
                { Amount: <= 0 } => "Expense amount must be greater than zero.",
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
        public async Task<RoomExpenseResponse> GetRoomExpensesForApi(int roomId, DateTime selectedMonth, bool includeRoomInfo = true)
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

            if (cachedData is null)
                return new RoomExpenseResponse();

            var expenses = cachedData.Expenses ?? new List<Expense>();
            var settlements = cachedData.Settlements ?? new List<Settlement>();

            var userId = currentUser.UserId;
            List<Member> members = await memberRepository.GetMembersByRoomId(roomId, userId);
            string? roomName = roomRepository.GetRoomName(roomId);

            int year, month;
            if (selectedMonth == DateTime.MinValue && expenses.Count > 0)
            {
                var latestExpense = expenses.OrderByDescending(e => e.Date).First();
                year = latestExpense.Date.Year;
                month = latestExpense.Date.Month;
            }
            else
            {
                year = selectedMonth.Year;
                month = selectedMonth.Month;
            }

            var filteredExpenses = expenses
                .Where(e => e.Date.Year == year && e.Date.Month == month)
                .OrderBy(e => e.Date)
                .ToList();

            var totalExpense = filteredExpenses.Sum(e => e.Amount);

            var availableMonths = expenses
                .Select(e => e.Date.ToString("yyyy-MM"))
                .Distinct()
                .OrderByDescending(m => m)
                .ToList();

            var response = new RoomExpenseResponse
            {
                TotalExpense = totalExpense,
                SelectedMonth = $"{year:D4}-{month:D2}",
                Expenses = filteredExpenses.Select(e => new ExpenseDetailResponse
                {
                    ExpenseId = e.ExpenseId,
                    RoomId = e.RoomId,
                    Description = e.Item ?? string.Empty,
                    Amount = e.Amount,
                    Date = e.Date,
                    PayerName = e.Member.Name,
                    PayerId = e.Member.MemberId,
                    Category = e.Item ?? string.Empty,
                    IconName = MapCategoryToIcon(e.Item ?? string.Empty),
                    Status = "" // Fill in actual status if needed
                }).ToList()
            };

            if (includeRoomInfo && members.Any())
            {
                response.RoomId = roomId;
                response.RoomName = roomName ?? string.Empty;
                response.MembersData = members.Select(m => new MemberData
                {
                    MemberId = m.MemberId,
                    MemberName = m.Name
                }).ToList();
                response.AvailableMonths = availableMonths;
            }

            return response;
        }
        public async Task<List<UserExpenseResponse>> GetUserExpensesForApi()
        {
            try
            {
                var userId = currentUser.UserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return new List<UserExpenseResponse>();
                }

                var now = DateTime.Now; 
                var startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-1); 
                var endDate = new DateTime(now.Year, now.Month, 1).AddMonths(1).AddDays(-1);

                string cacheKey = $"UserExpenses_{userId}_{startDate:yyyy-MM}_{endDate:yyyy-MM}";

                if (!cache.TryGetValue(cacheKey, out List<UserExpenseResponse>? cachedData))
                {
                    var expenses = await expenseRepository.GetUserExpenses(userId, startDate, endDate);

                    cachedData = expenses.Select(e => new UserExpenseResponse
                    {
                        Item = e.Item ?? string.Empty,
                        RoomName = e.Room?.Name ?? string.Empty,
                        Amount = e.Amount,
                        ExpenseDate = e.Date,
                        IconName = MapCategoryToIcon(e.Item ?? string.Empty),
                        UserId = userId
                    }).ToList();

                    cache.Set(cacheKey, cachedData, TimeSpan.FromDays(30));
                }
                return cachedData ?? new List<UserExpenseResponse>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user expenses: {ex.Message}");
                return new List<UserExpenseResponse>();
            }
        }
        private string MapCategoryToIcon(string category)
        {
            return category switch
            {
                "Non-Veg" => "restaurant",
                "Dairy" => "cafe",
                "Pulses" => "nutrition",
                "Grains" => "rice",
                "Cooking Essentials" => "flash",
                "Vegetables" => "leaf",
                "Utilities" => "water",
                "Household Supplies" => "broom",
                "Ready-made Food" => "fast-food",
                "Bills" => "document-text",
                "Other" => "wallet",
                _ => "wallet"
            };
        }
        private string GetCategoryFromItem(string item)
        {
            if (string.IsNullOrWhiteSpace(item))
                return "Other";

            item = item.ToLower();

            // Define keywords and their categories
            var categoryKeywords = new Dictionary<string, string>()
            {
                { "chicken", "Non-Veg" },
                { "beef", "Non-Veg" },
                { "meat", "Non-Veg" },
                { "milk", "Dairy" },
                { "egg", "Dairy" },
                { "curd", "Dairy" },
                { "dahi", "Dairy" },
                { "dal", "Pulses" },
                { "rajma", "Pulses" },
                { "chana", "Pulses" },
                { "moong", "Pulses" },
                { "arhar", "Pulses" },
                { "rice", "Grains" },
                { "chawal", "Grains" },
                { "oil", "Cooking Essentials" },
                { "ghee", "Cooking Essentials" },
                { "vegetable", "Vegetables" },
                { "bhindi", "Vegetables" },
                { "aaloo", "Vegetables" },
                { "tamatar", "Vegetables" },
                { "kheera", "Vegetables" },
                { "onion", "Vegetables" },
                { "patti", "Vegetables" },
                { "water", "Utilities" },
                { "paani", "Utilities" },
                { "surf", "Household Supplies" },
                { "sabun", "Household Supplies" },
                { "swiggy", "Ready-made Food" },
                { "instamart", "Ready-made Food" },
                { "snack", "Ready-made Food" },
                { "bakery", "Ready-made Food" },
                { "current bill", "Bills" },
                { "electricity", "Bills" }
            };

            foreach (var kvp in categoryKeywords)
            {
                if (item.Contains(kvp.Key))
                    return kvp.Value;
            }

            return "Other";
        }
    }
}
