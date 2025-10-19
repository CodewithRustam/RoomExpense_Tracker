using Domain.AppUser;
using Domain.Entities;
using Domain.Interfaces;
using ExpenseTrakcerHepler;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Services.Interfaces;
using Services.ViewModels;
using Services.ViewModels.ApiViewModels;
using System.Globalization;

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
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IServiceProvider _serviceProvider; 

        public ExpenseService(IExpenseRepository _expenseRepository, ISettlementRepository _settlementRepository, IMemberRepository _memberRepository, IRoomRepository _roomRepository, ICurrentUserService _currentUser, IMemoryCache _cache, UserManager<ApplicationUser> userManager, IServiceProvider serviceProvider) 
		{
            expenseRepository = _expenseRepository;
            settlementRepository = _settlementRepository;
            memberRepository = _memberRepository;
            roomRepository = _roomRepository;
            currentUser = _currentUser;
            cache = _cache;
            _userManager = userManager;
            _serviceProvider = serviceProvider; 
        }
        public async Task<string> AddExpenses(ExpenseViewModel expenseViewModel)
        {
            string message = string.Empty;
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
                        Category = CategoryMapper.GetCategoryFromItem(expenseViewModel.Item ?? string.Empty)
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

                        try
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
                            var deviceTokens = expenseRepository.GetDeviceToken(expense.RoomId);
                            var roomName = roomRepository.GetRoomName(expense.RoomId);
                            var memberName = currentUser.UserName ?? string.Empty;

                            await notificationService.SendExpenseNotificationAsync(deviceTokens!, expense.Item, expense.Amount, memberName, roomName);
                        }
                        catch
                        {
                            // swallow here: notifications must not break the request flow
                        }
                    }
                }
                else
                {
                    message = "Expense data is missing.";
                }
            }
            return message;
        }
        public async Task<string> UpdateExpenses(ExpenseViewModel expenseViewModel)
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
                        Category = CategoryMapper.GetCategoryFromItem(expenseViewModel.Item ?? string.Empty)
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
            return string.Empty;
        }

        public async Task<RoomExpensesViewModel> GetMonthlyExpenses(int roomId, DateTime selectedMonth)
        {
            RoomExpensesViewModel roomExpensesViewModel = new RoomExpensesViewModel();
            List<Expense>? expenses = await expenseRepository.GetMonthlyExpenses(roomId, selectedMonth);
            List<Settlement>? settlements = await settlementRepository.GetMonthlySettlements(roomId, selectedMonth);

            if (expenses is null || settlements is null)
            {
                expenses = new List<Expense>();
                settlements = new List<Settlement>();
            }
            var userId = currentUser.UserId;
            List<Member> members = await memberRepository.GetMembersByRoomId(roomId, userId);

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
            return summaries;
        }
        public async Task<RoomExpenseResponse> GetRoomExpensesForApi(int roomId, DateTime selectedMonth, bool includeRoomInfo = true)
        {
            if (selectedMonth == DateTime.MinValue)
            {
                selectedMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            List<Expense>? expenses = await expenseRepository.GetMonthlyExpenses(roomId, selectedMonth);
            List<Settlement>? settlements = await settlementRepository.GetMonthlySettlements(roomId, selectedMonth);

            if (expenses is null || settlements is null)
            {
                expenses = new List<Expense>();
                settlements = new List<Settlement>();
            }

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

            var filteredExpenses = expenses.Where(e => e.Date.Year == year && e.Date.Month == month)
                                           .OrderByDescending(e => e.Date).ToList();

            var filteredSettlements = settlements.Where(x => x.SettlementForDate.Year == year && x.SettlementForDate.Month == month)
                                                 .OrderByDescending(x => x.SettlementId).ToList();

            var totalExpense = filteredExpenses.Sum(e => e.Amount);

            var response = new RoomExpenseResponse
            {
                RoomId = roomId,
                TotalMontlyExpense = totalExpense,
                SelectedMonth = $"{year:D4}-{month:D2}",
                Expenses = filteredExpenses.Select(e => new ExpenseDetailResponse
                {
                    ExpenseId = e.ExpenseId,
                    RoomId = e.RoomId,
                    Item = e.Item ?? string.Empty,
                    Amount = e.Amount,
                    Date = e.Date,
                    PayerName = e.Member.Name,
                    PayerId = e.Member.MemberId,
                    Category = e.Category ?? string.Empty,
                    IconName = CategoryMapper.GetIconForCategory(e.Category ?? string.Empty),
                    IsEditShow = e.Member.ApplicationUserId == userId && DateTime.Now.Month == e.Date.Month
                }).ToList()
            };
            response.MembersSummary = CalculateMemberExpenseSummary(filteredExpenses, filteredSettlements, members);

            if (includeRoomInfo && members.Count > 0)
            {
                response.RoomName = roomName ?? string.Empty;
                response.AvailableMonths = expenses.Select(e => e.Date.ToString("yyyy-MM")).Distinct().OrderByDescending(m => m).ToList();
            }
            return response;
        }
        private List<MemberExpenseSummary> CalculateMemberExpenseSummary(List<Expense> expenses,List<Settlement> settlements,List<Member> members)
        {
            var summaries = new List<MemberExpenseSummary>();
            var userId = currentUser.UserId;

            if (members == null || !members.Any())
                return summaries;

            var totalExpenses = expenses.Where(e => !e.IsNonSplitExpense).Sum(e => e.Amount);
            var memberCount = members.Count;
            var avgShare = memberCount > 0 ? Math.Round(totalExpenses / memberCount, 2) : 0m;

            foreach (var member in members)
            {
                var totalMemebrExpense = expenses.Where(e => e.MemberId == member.MemberId && !e.IsNonSplitExpense).Sum(e => e.Amount);

                var amountPaid = settlements.Where(s => s.MemberId == member.MemberId).Sum(s => s.Amount);

                var amountReceived = settlements.Where(s => s.PaidToMemberId == member.MemberId).Sum(s => s.Amount);

                decimal netBalance = (totalMemebrExpense + amountPaid) - amountReceived - avgShare;

                string badgeText;
                decimal badgeAmount = Math.Abs(netBalance);

                if (Math.Abs(netBalance) < 0.5m)
                {
                    badgeText = "Settled up";
                    badgeAmount = 0;
                    netBalance = 0;
                }
                else if (netBalance > 0)
                {
                    badgeText = "Owed";
                }
                else
                {
                    badgeText = "Owe";
                }

                summaries.Add(new MemberExpenseSummary
                {
                    MemberId = member.MemberId,
                    MemberName = member.Name,
                    TotalMemberExpense = totalMemebrExpense,
                    AmountPaid = amountPaid,
                    AmountReceived = amountReceived,
                    NetBalance = netBalance,
                    BadgeText = badgeText,
                    BadgeAmount = badgeAmount,
                    IsSettleShow = member.ApplicationUserId == userId
                });
            }
            return summaries;
        }

        public async Task<UserExpenseDetails> GetUserExpensesForApi()
        {
            var userId = currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return new UserExpenseDetails();
            }

            var now = DateTime.Now;
            var startDate = new DateTime(now.Year, now.Month, 1).AddMonths(-1);
            var endDate = new DateTime(now.Year, now.Month, 1).AddMonths(1).AddDays(-1);

            string cacheKey = $"UserExpenses_{userId}__{startDate:yyyy-MM}_{endDate:yyyy-MM}";

            //if (!cache.TryGetValue(cacheKey, out List<UserExpenseResponse>? cachedData))
            //{
            var expenses = await expenseRepository.GetUserExpenses(userId, startDate, endDate);
            List<string> months = expenses.Select(e => e.Date.ToString("yyyy-MM")).Distinct().OrderByDescending(m => m).ToList();

            UserExpenseDetails userExpenseDetails = new UserExpenseDetails();
            var userExpenseRes = expenses.Select(e => new UserExpenseResponse
            {
                Item = e.Item ?? string.Empty,
                RoomName = e.Room?.Name ?? string.Empty,
                Amount = e.Amount,
                ExpenseDate = e.Date,
                IconName = CategoryMapper.GetIconForCategory(e.Category ?? string.Empty),
                UserId = userId
            }).ToList();

            userExpenseDetails.UserExpenseResponse = userExpenseRes;
            userExpenseDetails.Months = months;

            //cache.Set(cacheKey, cachedData, TimeSpan.FromDays(30));
            //}
            return userExpenseDetails ?? new UserExpenseDetails();
        }
        public async Task<MonthlyExpensesTrendResponse> GetMonthlyExpensesTrend(int roomId, string month)
        {
            var userId = currentUser.UserId;
            var members = await memberRepository.GetMembersByRoomId(roomId, userId);
            var expenses = await expenseRepository.GetAllAsync(x => x.RoomId == roomId);
            var settlements = await settlementRepository.GetAllAsync();

            // Parse the input month (e.g., "2025-10")
            if (!DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var targetMonth))
            {
                throw new ArgumentException("Invalid month format. Use YYYY-MM.");
            }

            // Filter expenses and settlements for the specified month
            expenses = expenses.Where(e => e.Date.Year == targetMonth.Year && e.Date.Month == targetMonth.Month).ToList();
            settlements = settlements.Where(s => s.SettlementForDate.Year == targetMonth.Year && s.SettlementForDate.Month == targetMonth.Month).ToList();

            // Build single-month list
            var months = new List<string> { targetMonth.ToString("MMMM yyyy") }; // e.g., "October 2025"

            var response = new MonthlyExpensesTrendResponse
            {
                Months = months,
                Members = new List<MemberExpenses>(),
                CategoryExpenses = new List<CategoryMonthlyExpense>(),
                TopSpends = new List<TopSpend>()
            };

            // Member-wise monthly trend (single month)
            foreach (var member in members)
            {
                var memberExpenses = expenses
                    .Where(e => e.MemberId == member.MemberId && (e.IsDeleted == false || e.IsDeleted == null))
                    .Sum(e => e.Amount);

                var paidSettlements = settlements
                    .Where(s => s.MemberId == member.MemberId)
                    .Sum(s => s.Amount);

                var receivedSettlements = settlements
                    .Where(s => s.PaidToMemberId == member.MemberId)
                    .Sum(s => s.Amount);

                var netExpense = memberExpenses - paidSettlements + receivedSettlements;

                response.Members.Add(new MemberExpenses
                {
                    Name = member.Name,
                    MonthlyExpenses = new List<decimal> { netExpense }
                });
            }

            // Category-wise monthly totals (single month)
            var categories = expenses
                .Where(e => e.IsDeleted == false || e.IsDeleted == null)
                .Select(e => e.Category)
                .Distinct()
                .ToList();

            foreach (var category in categories)
            {
                var total = expenses
                    .Where(e => e.Category == category && (e.IsDeleted == false || e.IsDeleted == null))
                    .Sum(e => e.Amount);

                response.CategoryExpenses.Add(new CategoryMonthlyExpense
                {
                    CategoryName = category!,
                    MonthlyTotals = new List<decimal> { total },
                    IconName = CategoryMapper.GetIconForCategory(category ?? string.Empty)
                });
            }

            // Top Spends for the month
            var topSpendsForMonth = expenses
                .Where(e => e.IsDeleted == false || e.IsDeleted == null)
                .GroupBy(e => e.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .OrderByDescending(x => x.TotalAmount)
                .Take(5)
                .ToList();

            foreach (var spend in topSpendsForMonth)
            {
                response.TopSpends.Add(new TopSpend
                {
                    CategoryName = spend.Category!,
                    MonthlyTotals = new List<decimal> { spend.TotalAmount },
                    TotalAmount = spend.TotalAmount,
                    IconName = CategoryMapper.GetIconForCategory(spend.Category ?? string.Empty)
                });
            }

            return response;
        }
        // New method for getting settlement details
        public async Task<SettlementData> GetSettlementDetails(int roomId, int memberId, DateTime? targetMonth = null)
        {
            // Check if user is authorized to access this room
            var userId = currentUser.UserId;
            var members = await memberRepository.GetMembersByRoomId(roomId, userId);

            var room = await roomRepository.GetByIdAsync(roomId);
            
            var targetMember = members.FirstOrDefault(m => m.MemberId == memberId);
           
            // Fetch expenses and settlements
            List<Expense> expenses = targetMonth.HasValue
                ? await expenseRepository.GetMonthlyExpenses(roomId, targetMonth.Value)
                : await expenseRepository.GetAllAsync(x => x.RoomId == roomId && (x.IsDeleted == false || x.IsDeleted == null));

            List<Settlement> settlements = targetMonth.HasValue
                ? await settlementRepository.GetMonthlySettlements(roomId, targetMonth.Value)
                : await settlementRepository.GetAllAsync(s => s.RoomId == roomId);

            int year, month;
            if (targetMonth == null || targetMonth == DateTime.MinValue && expenses.Count > 0)
            {
                var latestExpense = expenses.OrderByDescending(e => e.Date).First();
                year = latestExpense.Date.Year;
                month = latestExpense.Date.Month;
            }
            else
            {
                year = targetMonth.Value.Year;
                month = targetMonth.Value.Month;
            }

            var filteredExpenses = expenses.Where(e => e.Date.Year == year && e.Date.Month == month)
                                           .OrderByDescending(e => e.Date).ToList();

            var filteredSettlements = settlements.Where(x => x.SettlementForDate.Year == year && x.SettlementForDate.Month == month)
                                                 .OrderByDescending(x => x.SettlementId).ToList();

            expenses = filteredExpenses ?? new List<Expense>();
            settlements = filteredSettlements ?? new List<Settlement>();

            // Calculate net balances
            var totalExpenses = expenses.Where(e => !e.IsNonSplitExpense).Sum(e => e.Amount);
            var memberCount = members.Count;
            var avgShare = memberCount > 0 ? Math.Round(totalExpenses / memberCount, 2) : 0m;

            var balances = new Dictionary<int, decimal>();
            foreach (var member in members)
            {
                var totalMemberExpense = expenses
                    .Where(e => e.MemberId == member.MemberId && !e.IsNonSplitExpense)
                    .Sum(e => e.Amount);

                var amountPaid = settlements
                    .Where(s => s.MemberId == member.MemberId)
                    .Sum(s => s.Amount);

                var amountReceived = settlements
                    .Where(s => s.PaidToMemberId == member.MemberId)
                    .Sum(s => s.Amount);

                var netBalance = (totalMemberExpense + amountPaid) - amountReceived - avgShare;
                balances[member.MemberId] = Math.Abs(netBalance) < 0.5m ? 0m : netBalance;
            }

            // Compute settlements for the target member using a greedy algorithm
            var settlementDetails = ComputeSettlementsForMember(balances, members, memberId);

            var settlementData = new SettlementData
            {
                NetBalance = balances.ContainsKey(memberId) ? balances[memberId] : 0m,
                Settlements = settlementDetails
            };

            return settlementData;
        }

        private List<SettlementDetail> ComputeSettlementsForMember(Dictionary<int, decimal> balances, List<Member> members, int targetMemberId)
        {
            var settlements = new List<SettlementDetail>();
            var debtorBalance = balances.ContainsKey(targetMemberId) ? balances[targetMemberId] : 0m;

            if (Math.Abs(debtorBalance) < 0.5m)
            {
                return settlements; // Already settled
            }

            // Prepare creditors (positive balance) and other debtors (negative balance)
            var creditors = new List<(int Id, decimal Balance, string Name)>();
            var otherDebtors = new List<(int Id, decimal Balance, string Name)>();

            foreach (var member in members.Where(m => m.MemberId != targetMemberId))
            {
                var balance = balances.ContainsKey(member.MemberId) ? balances[member.MemberId] : 0m;
                if (balance > 0)
                {
                    creditors.Add((member.MemberId, balance, member.Name));
                }
                else if (balance < 0)
                {
                    otherDebtors.Add((member.MemberId, Math.Abs(balance), member.Name));
                }
            }

            // Sort by balance (descending for creditors, debtors)
            creditors = creditors.OrderByDescending(c => c.Balance).ToList();
            otherDebtors = otherDebtors.OrderByDescending(d => d.Balance).ToList();

            if (debtorBalance < 0)
            {
                // Target member owes money
                var amountToPay = Math.Abs(debtorBalance);
                foreach (var creditor in creditors)
                {
                    if (amountToPay <= 0) break;
                    var settleAmount = Math.Min(amountToPay, creditor.Balance);
                    if (settleAmount > 0)
                    {
                        settlements.Add(new SettlementDetail
                        {
                            ToMemberId = creditor.Id,
                            ToMemberName = creditor.Name,
                            Amount = Math.Round(settleAmount, 2)
                        });
                        amountToPay -= settleAmount;
                    }
                }
                // If still owes, handle other debtors (unlikely in equal splits, but included for robustness)
                foreach (var otherDebtor in otherDebtors)
                {
                    if (amountToPay <= 0) break;
                    var settleAmount = Math.Min(amountToPay, otherDebtor.Balance);
                    if (settleAmount > 0)
                    {
                        settlements.Add(new SettlementDetail
                        {
                            ToMemberId = otherDebtor.Id,
                            ToMemberName = otherDebtor.Name,
                            Amount = Math.Round(settleAmount, 2)
                        });
                        amountToPay -= settleAmount;
                    }
                }
            }
            else if (debtorBalance > 0)
            {
                // Target member is owed money
                var amountToReceive = debtorBalance;
                foreach (var otherDebtor in otherDebtors)
                {
                    if (amountToReceive <= 0) break;
                    var settleAmount = Math.Min(amountToReceive, otherDebtor.Balance);
                    if (settleAmount > 0)
                    {
                        settlements.Add(new SettlementDetail
                        {
                            ToMemberId = otherDebtor.Id,
                            ToMemberName = otherDebtor.Name,
                            Amount = Math.Round(settleAmount, 2)
                        });
                        amountToReceive -= settleAmount;
                    }
                }
            }

            return settlements;
        }
    }
}
