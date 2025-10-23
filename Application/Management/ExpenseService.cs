using Azure;
using Domain.AppUser;
using Domain.Entities;
using Domain.Entities.Dto;
using Domain.Interfaces;
using ExpenseTrakcerHepler;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Services.Interfaces;
using Services.ViewModels;
using Services.ViewModels.ApiViewModels;
using System.Globalization;
using static Services.Management.ExpenseService;

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
        public async Task<List<string>?> GetExpenseMonths(int roomId)
        {
            return await expenseRepository.GetExpenseMonths(roomId);
        }
        public async Task<ApiResponse> AddExpenses(ExpenseViewModel expenseViewModel)
        {
            string message = string.Empty;
            var error = ValidateExpenseViewModel(expenseViewModel);

            if (!string.IsNullOrEmpty(error))
                return ApiResponse.Fail(error);

            string? userId = currentUser.UserId;
            int memberId = await memberRepository.GetMemberId(userId, expenseViewModel.RoomId);
            if (memberId == 0) return ApiResponse.Fail("Member not found.");

            expenseViewModel.MemberId = memberId;

            var rateLimitKey = $"AddExpense-{userId}";

            if (cache.TryGetValue(rateLimitKey, out int count))
            {
                if (count >= 3)
                {
                    return ApiResponse.Fail("You are submitting too many expenses at once. Please wait a minute.");
                }
                cache.Set(rateLimitKey, count + 1, TimeSpan.FromMinutes(1));
            }
            else
            {
                cache.Set(rateLimitKey, 1, TimeSpan.FromMinutes(1));
            }

            if (expenseViewModel is null)
            {
                return ApiResponse.Fail("Expense details are missing.");
            }
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
                return ApiResponse.Fail("This expense already exists.");
            }

            message = await expenseRepository.AddExpenses(expense);
            if (expense is not null && expense.ExpenseId > 0)
            {
                var cacheKey = CacheHepler.GetCacheKey(expenseViewModel.RoomId, expenseViewModel.Date);
                cache.Remove(cacheKey);

                foreach (var isMemberInclude in new[] { true, false })
                {
                    string expenseDetails = CacheHepler.GetMonthlyExpensesKey(expenseViewModel.RoomId, expenseViewModel.Date, isMemberInclude);
                    cache.Remove(expenseDetails);
                }

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
                return ApiResponse.SuccessRes("Expense has been recorded successfully.");
            }
            else
            {
                return ApiResponse.Fail("Failed to add expense. Please try again later.");
            }
        }
        public async Task<ApiResponse> UpdateExpenses(ExpenseViewModel expenseViewModel)
        {
            var error = ValidateExpenseViewModel(expenseViewModel);

            if (!string.IsNullOrEmpty(error))
                return ApiResponse.Fail(error);

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

                foreach (var isMemberInclude in new[] { true, false })
                {
                    string expenseDetails = CacheHepler.GetMonthlyExpensesKey(expenseViewModel.RoomId, expenseViewModel.Date, isMemberInclude);
                    cache.Remove(expenseDetails);
                }
                return ApiResponse.SuccessRes(result.Message);
            }
            else
            {
                return ApiResponse.Fail(result.Message);
            }
        }

        public async Task<RoomExpensesViewModel> GetMonthlyExpenses(int roomId, DateTime selectedMonth)
        {
            RoomExpensesViewModel roomExpensesViewModel = new RoomExpensesViewModel();
            List<Expense>? expenses = new List<Expense>() ; //= await expenseRepository.GetMonthlyExpenses(roomId, selectedMonth);
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

        string ValidateExpenseViewModel(ExpenseViewModel viewModel)
        {
            return viewModel switch
            {
                null => "Expense details are missing.",
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
        public async Task<ApiResponse> GetRoomExpensesForApi(int roomId, DateTime selectedMonth, bool includeRoomInfo = true)
        {
            string cacheKey = CacheHepler.GetMonthlyExpensesKey(roomId, selectedMonth, includeRoomInfo);
            if (!cache.TryGetValue(cacheKey, out RoomExpenseResponse? response))
            {
                List<ExpenseRecordDto>? expenses = await expenseRepository.GetMonthlyExpenses(roomId, selectedMonth);
                List<Settlement>? settlements = await settlementRepository.GetMonthlySettlements(roomId, selectedMonth);

                var userId = currentUser.UserId;
                List<Member> members = await memberRepository.GetMembersByRoomId(roomId, userId);
                string? roomName = roomRepository.GetRoomName(roomId);

                var filteredSettlements = settlements.OrderByDescending(x => x.SettlementId).ToList();

                var totalExpense = expenses.Sum(e => e.Amount);

                int year = selectedMonth.Year;
                int month = selectedMonth.Month;

                response = new RoomExpenseResponse
                {
                    RoomId = roomId,
                    TotalMontlyExpense = totalExpense,
                    SelectedMonth = $"{year:D4}-{month:D2}",
                    Expenses = expenses.Select(e => new ExpenseDetailResponse
                    {
                        ExpenseId = e.ExpenseId,
                        RoomId = e.RoomId,
                        Item = e.Item ?? string.Empty,
                        Amount = e.Amount,
                        Date = e.Date,
                        PayerName = e.PayerName,
                        PayerId = e.PayerId,
                        Category = e.Category ?? string.Empty,
                        IconName = CategoryMapper.GetIconForCategory(e.Category ?? string.Empty),
                        IsEditShow = e.ApplicationUserId == userId && DateTime.Now.Month == e.Date.Month
                    }).ToList()
                };
                response.MembersSummary = CalculateMemberExpenseSummary(expenses, filteredSettlements, members);

                if (includeRoomInfo && members.Count > 0)
                {
                    response.RoomName = roomName ?? string.Empty;
                    response.AvailableMonths = await expenseRepository.GetExpenseMonths(roomId);
                }
                cache.Set(cacheKey, response, TimeSpan.FromDays(30));
            }
            if (response is null)
            {
                return ApiResponse<RoomExpenseResponse>.SuccessRes(response, "Failed to fetch expense details. Please try again later.");
            }
            return ApiResponse<RoomExpenseResponse>.SuccessRes(response, "Expense details fetched successfully.");
        }
        private List<MemberExpenseSummary> CalculateMemberExpenseSummary(List<ExpenseRecordDto> expenses,List<Settlement> settlements,List<Member> members)
        {
            var summaries = new List<MemberExpenseSummary>();
            var userId = currentUser.UserId;

            if (members == null || !members.Any())
                return summaries;

            var totalExpenses = expenses.Sum(e => e.Amount);
            var memberCount = members.Count;
            var avgShare = memberCount > 0 ? Math.Round(totalExpenses / memberCount, 2) : 0m;

            foreach (var member in members)
            {
                var totalMemebrExpense = expenses.Where(e => e.PayerId == member.MemberId).Sum(e => e.Amount);

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

        public async Task<ApiResponse> GetUserExpensesForApi(DateTime month)
        {
            var userId = currentUser.UserId;
            if (string.IsNullOrEmpty(userId)) return ApiResponse.Fail("User not found.");

            var expenses = await expenseRepository.GetUserExpenses(userId, month);

            if(expenses is null) return ApiResponse.Fail("User expenses not found.");

            var userExpenseRes = expenses.Select(e => new UserExpenseResponse
            {
                Item = e.Item ?? string.Empty,
                RoomName = e.RoomName ?? string.Empty,
                Amount = e.Amount,
                ExpenseDate = e.ExpenseDate,
                IconName = CategoryMapper.GetIconForCategory(e.Category ?? string.Empty),
                UserId = userId
            }).ToList();
            return ApiResponse.SuccessRes("User expenses fetched successfully.");
        }
        public async Task<ApiResponse> GetMonthlyExpensesTrend(int roomId, string month)
        {
            if (!DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var targetMonth))
                return ApiResponse.Fail("Invalid month format. Use YYYY-MM.");

            var (memberDtos, categoryDtos, topSpendsDtos) = await expenseRepository.GetMonthlyExpensesTrendAsync(roomId, targetMonth);

            if (memberDtos == null || categoryDtos == null || topSpendsDtos == null)
                return ApiResponse.Fail("Failed to fetch monthly expense trends. Please try again later.");

            if (!memberDtos.Any() && !categoryDtos.Any() && !topSpendsDtos.Any())
                return ApiResponse.Fail("No expense trend data found for the selected month.");

            var response = new MonthlyExpensesTrendResponse
            {
                Months = new List<string> { targetMonth.ToString("MMMM yyyy") },
                Members = memberDtos.Select(m => new MemberExpenses
                {
                    Name = m.Name,
                    MonthlyExpenses = new List<decimal> { m.TotalExpense }
                }).ToList(),

                CategoryExpenses = categoryDtos.Select(c => new CategoryMonthlyExpense
                {
                    CategoryName = c.Category,
                    MonthlyTotals = new List<decimal> { c.TotalAmount },
                    IconName = CategoryMapper.GetIconForCategory(c.Category)
                }).ToList(),

                TopSpends = topSpendsDtos.Select(t => new TopSpend
                {
                    CategoryName = t.Category,
                    MonthlyTotals = new List<decimal> { t.TotalAmount },
                    TotalAmount = t.TotalAmount,
                    IconName = CategoryMapper.GetIconForCategory(t.Category)
                }).ToList()
            };
            return ApiResponse.SuccessRes("Expense trend data fetched successfully.");
        }
        public async Task<ApiResponse> GetSettlementDetails(int roomId, int memberId, string month)
        {
            // ✅ Validate month format
            if (!DateTimeParser.ParseMonthYear(month, out var targetMonth))
            {
                return ApiResponse.Fail("Invalid month format. Please use YYYY-MM format (e.g., 2025-10).");
            }

            // ✅ Validate room
            if (roomId <= 0 || !await roomServices.IsValidRoomAsync(roomId))
            {
                return ApiResponse.Fail("Invalid room ID or room not found.");
            }

            // ✅ Validate members
            var userId = currentUser.UserId;
            var members = await memberRepository.GetMembersByRoomId(roomId, userId);
            if (members == null || !members.Any())
            {
                return ApiResponse.Fail("No members found for the specified room.");
            }

            // ✅ Get monthly settlement details
            var monthlyBalances = await settlementRepository.GetMonthlySettlementsDetails(roomId, targetMonth);
            if (monthlyBalances == null || !monthlyBalances.Any())
            {
                return ApiResponse.Fail("No settlement data found for the selected month.");
            }

            // ✅ Calculate balances
            var balancesDict = new Dictionary<int, decimal>(monthlyBalances.Count);
            decimal targetBalance = 0m;

            foreach (var m in monthlyBalances)
            {
                balancesDict[m.MemberId] = m.NetBalance;
                if (m.MemberId == memberId)
                    targetBalance = m.NetBalance;
            }

            // ✅ Compute settlements for the selected member
            var settlementDetails = ComputeSettlementsForMember(balancesDict, members, memberId);

            // ✅ Prepare response object
            var settlementData = new SettlementData
            {
                NetBalance = targetBalance,
                Settlements = settlementDetails
            };

            // ✅ Return success response
            return ApiResponse<SettlementData>.SuccessRes(settlementData, "Settlement details retrieved successfully.");
        }
        private List<SettlementDetail> ComputeSettlementsForMember(
            Dictionary<int, decimal> balancesDict,
            List<Member> members,
            int targetMemberId)
        {
            var settlements = new List<SettlementDetail>();

            if (!balancesDict.TryGetValue(targetMemberId, out decimal debtorBalance) || Math.Abs(debtorBalance) < 0.5m)
                return settlements; // Already settled or zero

            // Separate creditors and other debtors
            var creditors = new List<(int Id, decimal Balance, string Name)>();
            var otherDebtors = new List<(int Id, decimal Balance, string Name)>();

            foreach (var member in members)
            {
                if (member.MemberId == targetMemberId) continue;

                var balance = balancesDict[member.MemberId];
                if (balance > 0) creditors.Add((member.MemberId, balance, member.Name));
                else if (balance < 0) otherDebtors.Add((member.MemberId, Math.Abs(balance), member.Name));
            }

            creditors.Sort((a, b) => b.Balance.CompareTo(a.Balance));
            otherDebtors.Sort((a, b) => b.Balance.CompareTo(a.Balance));

            if (debtorBalance < 0)
            {
                // Target owes money
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
                            Amount = settleAmount
                        });
                        amountToPay -= settleAmount;
                    }
                }

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
                            Amount = settleAmount
                        });
                        amountToPay -= settleAmount;
                    }
                }
            }
            else
            {
                // Target is owed money
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
                            Amount = settleAmount
                        });
                        amountToReceive -= settleAmount;
                    }
                }
            }

            return settlements;
        }
    }
}
