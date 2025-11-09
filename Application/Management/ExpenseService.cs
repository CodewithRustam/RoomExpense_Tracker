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
        public async Task<List<string>?> GetExpenseMonthsByUserId()
        {
            return await expenseRepository.GetExpenseMonthsByUserId(currentUser.UserId!);
        }
        public async Task<ApiResponse> AddExpenses(ExpenseViewModel expenseViewModel)
        {
            var errorMsg = ValidateExpenseViewModel(expenseViewModel);

            if (!string.IsNullOrEmpty(errorMsg))
                return ApiResponse.Fail(errorMsg);

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

            string message = await expenseRepository.AddExpenses(expense);
            if (expense is not null && expense.ExpenseId > 0)
            {
                var cacheKey = CacheHelper.GetCacheKey(expenseViewModel.RoomId, expenseViewModel.Date);
                cache.Remove(cacheKey);
                string roomuserCacheKey = CacheHelper.GetRoomsUserKey(userId);
                cache.Remove(roomuserCacheKey);
                string monthlyTrendsCacheKey = CacheHelper.GetMonthlyExpenseTrendKey(expenseViewModel.RoomId, expenseViewModel.Date);
                cache.Remove(monthlyTrendsCacheKey);
                string userExpensecacheKey = CacheHelper.GetUserExpensesKey(userId, expenseViewModel.Date);
                cache.Remove(userExpensecacheKey);
                string settlementCacheKey = CacheHelper.GetSettlementCacheKey(expenseViewModel.RoomId, memberId, expenseViewModel.Date);
                cache.Remove(settlementCacheKey);

                foreach (var isMemberInclude in new[] { true, false })
                {
                    string expenseDetails = CacheHelper.GetMonthlyExpensesKey(expenseViewModel.RoomId, expenseViewModel.Date, isMemberInclude);
                    cache.Remove(expenseDetails);
                }

                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
                var deviceTokens = expenseRepository.GetDeviceToken(expense.RoomId, currentUser.UserId);
                var roomName = roomRepository.GetRoomName(expense.RoomId);
                var memberName = currentUser.UserName ?? string.Empty;

                await notificationService.SendExpenseNotificationAsync(deviceTokens!, expense.Item, expense.Amount, memberName, roomName,expense.RoomId);

                return ApiResponse.SuccessRes(message);
            }
            else
            {
                return ApiResponse.Fail(message);
            }
        }
        public async Task<ApiResponse> UpdateExpenses(ExpenseViewModel expenseViewModel)
        {
            var errorMsg = ValidateExpenseViewModel(expenseViewModel);

            if (!string.IsNullOrEmpty(errorMsg))
                return ApiResponse.Fail(errorMsg);

            bool isMonthSettledForRoom = await settlementRepository.IsMonthSettledForRoomAsync(expenseViewModel.RoomId, expenseViewModel.Date);

            if(isMonthSettledForRoom) return ApiResponse.Fail("Cannot update expense for a settled month.");

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
                var cacheKey = CacheHelper.GetCacheKey(expenseViewModel.RoomId, expenseViewModel.Date);
                cache.Remove(cacheKey);
                string roomuserCacheKey = CacheHelper.GetRoomsUserKey(currentUser.UserId);
                cache.Remove(roomuserCacheKey);
                string monthlyTrendsCacheKey = CacheHelper.GetMonthlyExpenseTrendKey(expenseViewModel.RoomId, expenseViewModel.Date);
                cache.Remove(monthlyTrendsCacheKey);
                string userExpensecacheKey = CacheHelper.GetUserExpensesKey(currentUser.UserId, expenseViewModel.Date);
                cache.Remove(userExpensecacheKey);
                string settlementCacheKey = CacheHelper.GetSettlementCacheKey(expenseViewModel.RoomId, expenseViewModel.MemberId, expenseViewModel.Date);
                cache.Remove(settlementCacheKey);
                foreach (var isMemberInclude in new[] { true, false })
                {
                    string expenseDetails = CacheHelper.GetMonthlyExpensesKey(expenseViewModel.RoomId, expenseViewModel.Date, isMemberInclude);
                    cache.Remove(expenseDetails);
                }

                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();
                var deviceTokens = expenseRepository.GetDeviceToken(expense.RoomId);
                var roomName = roomRepository.GetRoomName(expense.RoomId);
                var memberName = currentUser.UserName ?? string.Empty;

                await notificationService.SendExpenseNotificationAsync(deviceTokens!, expense.Item, expense.Amount, memberName, roomName,expense.RoomId,true);
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
        public async Task<ApiResponse> GetRoomExpensesForApi(int roomId, string? monthReq, bool includeRoomInfo = true)
        {
            RoomExpenseResponse? response = null;

            DateTime currentExpensemonth = new DateTime();
            if (string.IsNullOrEmpty(monthReq))
            {
                currentExpensemonth = expenseRepository.GetAllAsync(x => x.RoomId == roomId).Result.Max(x=>x.Date);
            }
            else
            {
                if (!DateTimeParser.ParseMonthYear(monthReq, out var targetMonth))
                {
                    return ApiResponse.Fail("Invalid month format. Please use YYYY-MM format (e.g., 2025-10).");
                }
                currentExpensemonth = targetMonth;
            }

            bool isCurrentMonth = currentExpensemonth.Year == DateTime.Now.Year &&
                                  currentExpensemonth.Month == DateTime.Now.Month;


            List<ExpenseRecordDto>? expenses = await expenseRepository.GetMonthlyExpenses(roomId, currentExpensemonth);
            List<Settlement>? settlements = await settlementRepository.GetMonthlySettlements(roomId, currentExpensemonth);

            var userId = currentUser.UserId;
            List<Member> members = await memberRepository.GetMembersByRoomId(roomId, userId);
            string? roomName = roomRepository.GetRoomName(roomId);

            var filteredSettlements = settlements.OrderByDescending(x => x.SettlementId).ToList();
            var totalExpense = expenses.Sum(e => e.Amount);

            int year = currentExpensemonth.Year;
            int month = currentExpensemonth.Month;

            bool isMonthSettledForRoom = await settlementRepository.IsMonthSettledForRoomAsync(roomId, currentExpensemonth);

            response = new RoomExpenseResponse
            {
                RoomId = roomId,
                TotalMontlyExpense = totalExpense,
                SelectedMonth =  $"{year:D4}-{month:D2}",
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
                    IsEditShow = e.ApplicationUserId == userId && !isMonthSettledForRoom
                }).ToList()
            };

            response.MembersSummary = CalculateMemberExpenseSummary(expenses, filteredSettlements, members);

            if (includeRoomInfo && members.Count > 0)
            {
                response.RoomName = roomName ?? string.Empty;
                response.AvailableMonths = await expenseRepository.GetExpenseMonths(roomId);
            }

            if (response is null)
            {
                return ApiResponse.Fail("Failed to fetch expense details. Please try again later.");
            }

            return ApiResponse<RoomExpenseResponse>.SuccessRes(response, "Expense details fetched successfully.");
        }
        List<MemberExpenseSummary> CalculateMemberExpenseSummary(List<ExpenseRecordDto> expenses,List<Settlement> settlements,List<Member> members)
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

        public async Task<ApiResponse> GetUserExpensesForApi(string month)
        {
            DateTime targetMonth;
            if (string.IsNullOrEmpty(month))
            {
                targetMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }
            else
            {
                if (!DateTimeParser.ParseMonthYear(month, out targetMonth))
                {
                    return ApiResponse.Fail("Invalid month format. Please use YYYY-MM format (e.g., 2025-10).");
                }
            }

            var userId = currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
                return ApiResponse.Fail("User not found.");

            string cacheKey = CacheHelper.GetUserExpensesKey(userId, targetMonth);

            if (cache.TryGetValue(cacheKey, out List<UserExpenseResponse>? cachedExpenses))
            {
                await Task.Delay(200);
                return ApiResponse<List<UserExpenseResponse>>.SuccessRes(cachedExpenses,
                    "User expenses fetched successfully.");
            }

            var expenses = await expenseRepository.GetUserExpenses(userId, targetMonth);
            if (expenses == null || !expenses.Any())
            {
                return ApiResponse.Fail("User expenses not found.");
            }

            var userExpenseRes = expenses.Select(e => new UserExpenseResponse
            {
                Item = e.Item ?? string.Empty,
                RoomName = e.RoomName ?? string.Empty,
                Amount = e.Amount,
                ExpenseDate = e.ExpenseDate,
                IconName = CategoryMapper.GetIconForCategory(e.Category ?? string.Empty),
                UserId = userId
            }).ToList();

            cache.Set(cacheKey, userExpenseRes, TimeSpan.FromDays(30));

            return ApiResponse<List<UserExpenseResponse>>.SuccessRes(userExpenseRes, "User expenses fetched successfully.");
        }
        public async Task<ApiResponse> GetMonthlyExpensesTrend(int roomId, string month)
        {
            if (!DateTimeParser.ParseMonthYear(month, out var targetMonth))
            {
                return ApiResponse.Fail("Invalid month format. Please use YYYY-MM format (e.g., 2025-10).");
            }

            string cacheKey = CacheHelper.GetMonthlyExpenseTrendKey(roomId, targetMonth);

            if (cache.TryGetValue(cacheKey, out MonthlyExpensesTrendResponse? cachedResponse))
            {
                await Task.Delay(200);
                return ApiResponse<MonthlyExpensesTrendResponse>.SuccessRes(cachedResponse,
                    "Expense trend data fetched successfully.");
            }

            var (memberDtos, categoryDtos, topSpendsDtos) = await expenseRepository.GetMonthlyExpensesTrendAsync(roomId, targetMonth);

            if (memberDtos == null || categoryDtos == null || topSpendsDtos == null)
            {
                return ApiResponse.Fail("Failed to fetch monthly expense trends. Please try again later.");
            }

            if (!memberDtos.Any() && !categoryDtos.Any() && !topSpendsDtos.Any())
            {
                return ApiResponse.Fail("No expense trend data found for the selected month.");
            }

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
            cache.Set(cacheKey, response, TimeSpan.FromDays(30));

            return ApiResponse<MonthlyExpensesTrendResponse>.SuccessRes(response, "Expense trend data fetched successfully.");
        }
        public async Task<ApiResponse> GetSettlementDetails(int roomId, int memberId, string month)
        {
            if (!DateTimeParser.ParseMonthYear(month, out var targetMonth))
            {
                return ApiResponse.Fail("Invalid month format. Please use YYYY-MM format (e.g., 2025-10).");
            }

            //string cacheKey = CacheHelper.GetSettlementCacheKey(roomId, memberId, targetMonth);
            //if (cache.TryGetValue(cacheKey, out SettlementData? cachedSettlementData))
            //{
            //    return ApiResponse<SettlementData>.SuccessRes(cachedSettlementData, "Settlement details retrieved.");
            //}

            if (roomId <= 0 || !await roomRepository.IsValidRoomAsync(roomId))
            {
                return ApiResponse.Fail("Invalid room ID or room not found.");
            }

            var userId = currentUser.UserId;
            var members = await memberRepository.GetMembersByRoomId(roomId, userId);
            if (members == null || !members.Any())
            {
                return ApiResponse.Fail("No members found for the specified room.");
            }

            var monthlyBalances = await settlementRepository.GetMonthlySettlementsDetails(roomId, targetMonth);
            if (monthlyBalances == null || !monthlyBalances.Any())
            {
                return ApiResponse.Fail("No settlement data found for the selected month.");
            }

            var balancesDict = new Dictionary<int, decimal>(monthlyBalances.Count);
            decimal targetBalance = 0m;
            foreach (var m in monthlyBalances)
            {
                balancesDict[m.MemberId] = m.NetBalance;
                if (m.MemberId == memberId)
                    targetBalance = m.NetBalance;
            }

            var settlementDetails = ComputeSettlementsForMember(balancesDict, members, memberId);

            var settlementData = new SettlementData
            {
                NetBalance = targetBalance,
                Settlements = settlementDetails
            };

            //cache.Set(cacheKey, settlementData, TimeSpan.FromDays(30));

            return ApiResponse<SettlementData>.SuccessRes(settlementData, "Settlement details retrieved successfully.");
        }
        private List<SettlementDetail> ComputeSettlementsForMember(Dictionary<int, decimal> balancesDict,List<Member> members,int targetMemberId)
        {
            var settlements = new List<SettlementDetail>();

            if (!balancesDict.TryGetValue(targetMemberId, out decimal targetBalance) || Math.Abs(targetBalance) < 0.5m)
                return settlements; 

            var creditors = new List<(int Id, decimal Balance, string Name)>();
            var debtors = new List<(int Id, decimal Balance, string Name)>();

            foreach (var member in members)
            {
                if (member.MemberId == targetMemberId) continue;

                var balance = balancesDict[member.MemberId];
                if (balance > 0) creditors.Add((member.MemberId, balance, member.Name));
                else if (balance < 0) debtors.Add((member.MemberId, Math.Abs(balance), member.Name));
            }

            if(creditors.Count > 0) creditors.Sort((a, b) => b.Balance.CompareTo(a.Balance));
            if(debtors.Count>0) debtors.Sort((a, b) => b.Balance.CompareTo(a.Balance));

            if (targetBalance < 0)
            {
                var amountToPay = Math.Abs(targetBalance);

                int i = 0;
                while (amountToPay > 0 && i < creditors.Count)
                {
                    var settleAmount = Math.Min(amountToPay, creditors[i].Balance);
                    if (settleAmount > 0)
                    {
                        settlements.Add(new SettlementDetail
                        {
                            ToMemberId = creditors[i].Id,
                            ToMemberName = creditors[i].Name,
                            Amount = settleAmount
                        });
                        amountToPay -= settleAmount;
                    }
                    i++;
                }

                i = 0;
                while (amountToPay > 0 && i < debtors.Count)
                {
                    var settleAmount = Math.Min(amountToPay, debtors[i].Balance);
                    if (settleAmount > 0)
                    {
                        settlements.Add(new SettlementDetail
                        {
                            ToMemberId = debtors[i].Id,
                            ToMemberName = debtors[i].Name,
                            Amount = settleAmount
                        });
                        amountToPay -= settleAmount;
                    }
                    i++;
                }
            }
            return settlements;
        }
    }
}
