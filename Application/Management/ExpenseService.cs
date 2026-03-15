using Infrastructure;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Services.Management
{
    public class ExpenseService : IExpenseServices
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IUnitOfWork _uow;
        private readonly ICurrentUserService _currentUser;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _config;
        private readonly ILogger<ExpenseService> _logger;

        #region Repositories
        private IExpenseRepository ExpenseRepo => (IExpenseRepository)_uow.Repository<Expense>();
        private IMemberRepository MemberRepo => (IMemberRepository)_uow.Repository<Member>();
        private ISettlementRepository SettlementRepo => (ISettlementRepository)_uow.Repository<Settlement>();
        private IRoomRepository RoomRepo => (IRoomRepository)_uow.Repository<Room>();
        #endregion

        public ExpenseService(
            IServiceScopeFactory scopeFactory,
            IUnitOfWork uow,
            ICurrentUserService currentUser,
            IMemoryCache cache,
            IConfiguration config,
            ILogger<ExpenseService> logger)
        {
            _scopeFactory = scopeFactory;
            _uow = uow;
            _currentUser = currentUser;
            _cache = cache;
            _config = config;
            _logger = logger;
        }

        #region Add Expense
        public async Task<ApiResponse> AddExpenses(ExpenseViewModel model)
        {
            var userId = _currentUser.UserId!;
            int memberId = await MemberRepo.GetMemberId(userId, model.RoomId);

            if (memberId == 0)
            {
                _logger.LogWarning("Unauthorized access attempt: User {UserId} is not a member of Room {RoomId}",
                    userId, model.RoomId);
                return ApiResponse.Fail("Member not found.");
            }

            if (IsRateLimited($"AddExpense-{userId}", 3, TimeSpan.FromMinutes(1)))
            {
                _logger.LogWarning("Rate limit exceeded for User {UserId} on AddExpense", userId);
                return ApiResponse.Fail("Too many expense submissions. Please wait.");
            }

            var expense = MapToExpense(model, memberId);

            if (await ExpenseRepo.IsExpenseExist(expense))
            {
                _logger.LogInformation("Duplicate expense ignored for User {UserId}, Room {RoomId}", userId, model.RoomId);
                return ApiResponse.Fail("This expense already exists.");
            }

            await ExpenseRepo.AddAsync(expense);

            if (await _uow.SaveAsync() <= 0)
            {
                _logger.LogError("Database commit failed for AddExpense. User: {UserId}, Room: {RoomId}", userId, model.RoomId);
                return ApiResponse.Fail("Failed to record expense.");
            }

            ClearExpenseCaches(model, userId, memberId);
            var userName = _currentUser.UserName;

            _ = Task.Run(async () =>
            {
                try
                {
                    await SendNotificationToUser(expense,userId, userName!);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background notification failed for expense {Id}", expense.ExpenseId);
                }
            });
            return ApiResponse.SuccessRes("Expense recorded successfully.");
        }
        #endregion

        #region Update Expense
        public async Task<ApiResponse> UpdateExpenses(ExpenseViewModel model)
        {
            if (await SettlementRepo.IsMonthSettledForRoomAsync(model.RoomId, model.Date))
            {
                _logger.LogWarning("Update blocked: Month is already settled for Room {RoomId}, Date {Date}. ExpenseId: {ExpenseId}",
                    model.RoomId, model.Date, model.ExpenseId);
                return ApiResponse.Fail("Cannot update expense for a settled month.");
            }

            var expense = MapToExpense(model, model.MemberId);
            expense.ExpenseId = model.ExpenseId ?? 0;

            if (!await ExpenseRepo.IsExpenseExistForUser(expense))
            {
                _logger.LogWarning("Unauthorized/Invalid update attempt: Expense {ExpenseId} does not exist or belong to User {UserId}",
                    expense.ExpenseId, _currentUser.UserId);
                return ApiResponse.Fail("Expense not found or permission denied.");
            }

            await ExpenseRepo.Update(expense);

            if (await _uow.SaveAsync() <= 0)
            {
                _logger.LogError("Database update failed for Expense {ExpenseId}. User: {UserId}",
                    expense.ExpenseId, _currentUser.UserId);
                return ApiResponse.Fail("Expense not updated.");
            }

            ClearExpenseCaches(model, _currentUser.UserId!, model.MemberId);

            _logger.LogInformation($"UpdateExpenses UserId: {_currentUser.UserId!}.");


            _ = Task.Run(async () =>
            {
                try
                {
                    await SendNotificationToUser(expense, _currentUser.UserId!, _currentUser.UserName!, isUpdate: true);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background notification failed for expense {Id}", expense.ExpenseId);
                }
            });

            return ApiResponse.SuccessRes("Expense updated successfully.");
        }
        #endregion

        #region Delete Expense
        public async Task<ApiResponse> DeleteExpense(int expenseId)
        {
            var userId = _currentUser.UserId!;
            string rateKey = $"RateLimit_Delete_{userId}";

            if (_cache.TryGetValue(rateKey, out int count) && count >= 2)
                return ApiResponse.Fail("Daily delete limit reached.");

            var expense = await ExpenseRepo.GetByIdAsync(expenseId);
            if (expense == null)
                return ApiResponse.Fail("Expense not found.");

            if (await SettlementRepo.IsMonthSettledForRoomAsync(expense.RoomId, expense.Date))
                return ApiResponse.Fail("Cannot delete from a settled month.");

            expense.IsDeleted = true;
            await ExpenseRepo.Update(expense);

            if (await _uow.SaveAsync() <= 0)
                return ApiResponse.Fail("Deletion failed.");

            _cache.Set(rateKey, count + 1, TimeSpan.FromDays(1));
            ClearAllRelatedCaches(expense, userId);

            return ApiResponse.SuccessRes("Expense deleted successfully.");
        }
        #endregion

        #region Get Expenses
        public async Task<List<string>?> GetExpenseMonthsByUserId()
        {
            return await ExpenseRepo.GetExpenseMonthsByUserId(_currentUser.UserId!);
        }

        public async Task<ApiResponse> GetRoomExpensesForApi(int roomId, string? monthReq, bool includeRoomInfo = true)
        {
            RoomExpenseResponse? response = null;

            DateTime currentExpensemonth = new DateTime();
            if (string.IsNullOrEmpty(monthReq))
            {
                var allExpenses = await ExpenseRepo.GetAllAsync(x => x.RoomId == roomId);

                if (allExpenses != null && allExpenses.Any())
                {
                    currentExpensemonth = allExpenses.Max(x => x.Date);
                }
                else
                {
                    currentExpensemonth = DateTime.Now;
                }
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


            List<ExpenseRecordDto>? expenses = await ExpenseRepo.GetMonthlyExpenses(roomId, currentExpensemonth);
            List<Settlement>? settlements = await SettlementRepo.GetMonthlySettlements(roomId, currentExpensemonth);

            var userId = _currentUser.UserId;
            List<Member> members = await MemberRepo.GetMembersByRoomId(roomId, userId);
            string? roomName = RoomRepo.GetRoomName(roomId);

            var filteredSettlements = settlements.OrderByDescending(x => x.SettlementId).ToList();
            var totalExpense = expenses.Sum(e => e.Amount);

            int year = currentExpensemonth.Year;
            int month = currentExpensemonth.Month;

            bool isMonthSettledForRoom = await SettlementRepo.IsMonthSettledForRoomAsync(roomId, currentExpensemonth);

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
                    IsEditShow = e.ApplicationUserId == userId && !isMonthSettledForRoom
                }).ToList()
            };

            response.MembersSummary = CalculateMemberExpenseSummary(expenses, filteredSettlements, members);

            if (includeRoomInfo && members.Count > 0)
            {
                response.RoomName = roomName ?? string.Empty;
                response.AvailableMonths = await ExpenseRepo.GetExpenseMonths(roomId);
            }

            if (response is null)
            {
                return ApiResponse.Fail("Failed to fetch expense details. Please try again later.");
            }

            return ApiResponse<RoomExpenseResponse>.SuccessRes(response, "Expense details fetched successfully.");
        }
        public async Task<ApiResponse> GetMonthlyExpensesTrend(int roomId, string month)
        {
            if (!DateTimeParser.ParseMonthYear(month, out var targetMonth))
                return ApiResponse.Fail("Invalid month format. Please use YYYY-MM format (e.g., 2025-10).");

            string cacheKey = CacheHelper.GetMonthlyExpenseTrendKey(roomId, targetMonth);

            if (_cache.TryGetValue(cacheKey, out MonthlyExpensesTrendResponse? cachedResponse))
            {
                await Task.Delay(200);
                return ApiResponse<MonthlyExpensesTrendResponse>.SuccessRes(cachedResponse,
                    "Expense trend data fetched successfully.");
            }

            var (memberDtos, categoryDtos, topSpendsDtos) = await ExpenseRepo.GetMonthlyExpensesTrendAsync(roomId, targetMonth);

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

            _cache.Set(cacheKey, response, TimeSpan.FromDays(30));

            return ApiResponse<MonthlyExpensesTrendResponse>.SuccessRes(response, "Expense trend data fetched successfully.");
        }

        public async Task<ApiResponse> GetSettlementDetails(int roomId, int memberId, string month)
        {
            if (!DateTimeParser.ParseMonthYear(month, out var targetMonth))
                return ApiResponse.Fail("Invalid month format. Please use YYYY-MM format (e.g., 2025-10).");

            if (roomId <= 0 || !await RoomRepo.IsValidRoomAsync(roomId))
                return ApiResponse.Fail("Invalid room ID or room not found.");

            var members = await MemberRepo.GetMembersByRoomId(roomId, _currentUser.UserId);
            if (members == null || !members.Any())
                return ApiResponse.Fail("No members found for the specified room.");

            var monthlyBalances = await SettlementRepo.GetMonthlySettlementsDetails(roomId, targetMonth);
            if (monthlyBalances == null || !monthlyBalances.Any())
                return ApiResponse.Fail("No settlement data found for the selected month.");

            var balancesDict = monthlyBalances.ToDictionary(m => m.MemberId, m => m.NetBalance);
            decimal targetBalance = balancesDict.GetValueOrDefault(memberId, 0m);

            var settlementDetails = ComputeSettlementsForMember(balancesDict, members, memberId);

            var settlementData = new SettlementData
            {
                NetBalance = targetBalance,
                Settlements = settlementDetails
            };

            return ApiResponse<SettlementData>.SuccessRes(settlementData, "Settlement details retrieved successfully.");
        }

        public async Task<ApiResponse> GetUserExpensesForApi(string month)
        {
            DateTime targetMonth = ParseTargetMonth(month);
            var userId = _currentUser.UserId;

            if (string.IsNullOrEmpty(userId))
                return ApiResponse.Fail("User not found.");

            string cacheKey = CacheHelper.GetUserExpensesKey(userId, targetMonth);

            if (_cache.TryGetValue(cacheKey, out List<UserExpenseResponse>? cachedExpenses))
            {
                await Task.Delay(200);
                return ApiResponse<List<UserExpenseResponse>>.SuccessRes(cachedExpenses,
                    "User expenses fetched successfully.");
            }

            var expenses = await ExpenseRepo.GetUserExpenses(userId, targetMonth);
            if (expenses == null || !expenses.Any())
                return ApiResponse.Fail("User expenses not found.");

            var userExpenseRes = expenses.Select(e => new UserExpenseResponse
            {
                Item = e.Item ?? string.Empty,
                RoomName = e.RoomName ?? string.Empty,
                Amount = e.Amount,
                ExpenseDate = e.ExpenseDate,
                IconName = CategoryMapper.GetIconForCategory(e.Category ?? string.Empty),
                UserId = userId
            }).ToList();

            _cache.Set(cacheKey, userExpenseRes, TimeSpan.FromDays(30));

            return ApiResponse<List<UserExpenseResponse>>.SuccessRes(userExpenseRes, "User expenses fetched successfully.");
        }
        public async Task<ApiResponse> GetHomeExpenseTrends(int roomId)
        {
            try
            {
                var startDate = DateTime.Now.AddMonths(-5);
                startDate = new DateTime(startDate.Year, startDate.Month, 1);

                var expenses = await ExpenseRepo.GetHomeExpenseTrends(roomId, startDate);

                var trendData = expenses
                    .GroupBy(e => new { e.Date.Year, e.Date.Month })
                    .Select(g => new MonthlyHomeTrendVM
                    {
                        MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM"),
                        Total = g.Sum(e => e.Amount)
                    })
                    .ToList();

                var finalTrend = new List<MonthlyHomeTrendVM>();
                for (int i = -5; i <= 0; i++)
                {
                    var targetDate = DateTime.Now.AddMonths(i);
                    var monthName = targetDate.ToString("MMM");
                    var existing = trendData.FirstOrDefault(x => x.MonthName == monthName);

                    finalTrend.Add(new MonthlyHomeTrendVM
                    {
                        MonthName = monthName,
                        Total = existing?.Total ?? 0
                    });
                }

                return ApiResponse<List<MonthlyHomeTrendVM>>.SuccessRes(finalTrend, "Home Expense Trends successfully.");
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        #endregion

        #region Helper Methods
        private List<SettlementDetail> ComputeSettlementsForMember(
            Dictionary<int, decimal> balancesDict,
            List<Member> members,
            int targetMemberId)
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

            if (creditors.Count > 0) creditors.Sort((a, b) => b.Balance.CompareTo(a.Balance));
            if (debtors.Count > 0) debtors.Sort((a, b) => b.Balance.CompareTo(a.Balance));

            if (targetBalance < 0)
            {
                var amountToPay = Math.Abs(targetBalance);
                int i = 0;

                // Pay to creditors first
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

                // If still amount left, pay to other debtors (rare edge case)
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

        private List<MemberExpenseSummary> CalculateMemberExpenseSummary(
            List<ExpenseRecordDto> expenses,
            List<Settlement> settlements,
            List<Member> members)
        {
            var summaries = new List<MemberExpenseSummary>();
            if (members == null || !members.Any()) return summaries;

            var totalExpenses = expenses.Sum(e => e.Amount);
            var memberCount = members.Count;
            var avgShare = memberCount > 0 ? Math.Round(totalExpenses / memberCount, 2) : 0m;
            var userId = _currentUser.UserId;

            foreach (var member in members)
            {
                var totalMemberExpense = expenses.Where(e => e.PayerId == member.MemberId).Sum(e => e.Amount);
                var amountPaid = settlements.Where(s => s.MemberId == member.MemberId).Sum(s => s.Amount);
                var amountReceived = settlements.Where(s => s.PaidToMemberId == member.MemberId).Sum(s => s.Amount);

                decimal netBalance = (totalMemberExpense + amountPaid) - amountReceived - avgShare;

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
                    TotalMemberExpense = totalMemberExpense,
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

        private Expense MapToExpense(ExpenseViewModel model, int memberId)
        {
            return new Expense
            {
                ExpenseId = model.ExpenseId ?? 0,
                MemberId = memberId,
                RoomId = model.RoomId,
                Item = model.Item?.Trim(),
                Amount = model.Amount,
                Date = model.Date.Date,
                Category = CategoryMapper.GetCategoryFromItem(model.Item ?? "")
            };
        }

        private bool IsRateLimited(string key, int limit, TimeSpan window)
        {
            if (_cache.TryGetValue(key, out int count) && count >= limit)
                return true;

            _cache.Set(key, count + 1, window);
            return false;
        }

        private string ValidateExpenseViewModel(ExpenseViewModel model)
        {
            if (model == null) return "Expense details are missing.";
            if (string.IsNullOrWhiteSpace(model.Item)) return "Expense item is required.";
            if (model.Amount <= 1) return "Amount must be greater than 1.";
            if (model.RoomId <= 0) return "Invalid room.";
            return string.Empty;
        }

        private void ClearExpenseCaches(ExpenseViewModel model, string userId, int memberId)
        {
            _cache.Remove(CacheHelper.GetCacheKey(model.RoomId, model.Date));
            _cache.Remove(CacheHelper.GetRoomsUserKey(userId));
            _cache.Remove(CacheHelper.GetUserExpensesKey(userId, model.Date));
            _cache.Remove(CacheHelper.GetMonthlyExpenseTrendKey(model.RoomId, model.Date));
            _cache.Remove(CacheHelper.GetSettlementCacheKey(model.RoomId, memberId, model.Date));

            foreach (var include in new[] { true, false })
            {
                _cache.Remove(CacheHelper.GetMonthlyExpensesKey(model.RoomId, model.Date, include));
            }
        }

        private void ClearAllRelatedCaches(Expense expense, string userId)
        {
            _cache.Remove(CacheHelper.GetCacheKey(expense.RoomId, expense.Date));
            _cache.Remove(CacheHelper.GetRoomsUserKey(userId));
            _cache.Remove(CacheHelper.GetUserExpensesKey(userId, expense.Date));
            _cache.Remove(CacheHelper.GetMonthlyExpenseTrendKey(expense.RoomId, expense.Date));
        }

        private DateTime ParseTargetMonth(string? month)
        {
            if (string.IsNullOrEmpty(month))
                return new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            if (!DateTimeParser.ParseMonthYear(month, out var targetMonth))
                throw new ArgumentException("Invalid month format. Please use YYYY-MM format (e.g., 2025-10).");

            return targetMonth;
        }
        #endregion

        #region Notifications
        private async Task SendNotificationToUser(Expense expense,string userId, string userName, bool isUpdate = false)
        {
            if (!Convert.ToBoolean(_config["EnableNotifications"]))
            {
                _logger.LogInformation("Notifications are disabled from configuration.");
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var notifService = scope.ServiceProvider.GetRequiredService<NotificationService>();
                var expenseRepo = scope.ServiceProvider.GetRequiredService<IExpenseRepository>();
                var memberRepo = scope.ServiceProvider.GetRequiredService<IMemberRepository>();
                var roomRepo = scope.ServiceProvider.GetRequiredService<IRoomRepository>();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var tokens = expenseRepo.GetDeviceToken(expense.RoomId, userId);

                if (tokens == null || !tokens.Any())
                {
                    _logger.LogWarning( "No device tokens found. Notifications will not be sent. RoomId={RoomId}",expense.RoomId);
                    return;
                }

                var roomName = roomRepo.GetRoomName(expense.RoomId);

                var result = await notifService.SendExpenseNotificationAsync(
                    tokens!,
                    expense.Item,
                    expense.Amount,
                    userName!,
                    roomName,
                    expense.RoomId,isUpdate);

                _logger.LogInformation("Push notification sent successfully. Body={Title}", result.Body);

                var members = await memberRepo.GetAllAsync(m => m.RoomId == expense.RoomId && m.ApplicationUserId != userId);

                if(members == null || !members.Any())
                {
                    _logger.LogWarning("No members found in the room for notification persistence. RoomId={RoomId}",expense.RoomId);
                    _logger.LogInformation("Fetched room members for notification persistence. MemberCount={MemberCount}", members?.Count());
                    return;
                }
                _logger.LogInformation($"UserId: {userId}, Fetched room members for notification persistence. MemberCount={members?.Count()}");

                var notifications = members!.Select(m => new PushNotification
                {
                    UserId = m.ApplicationUserId!,
                    Title = result.Title,
                    Body = result.Body,
                    SentAt = DateTimeProvider.NowIST,
                    IsRead = false
                }).ToList();

                if (!notifications.Any())
                {
                    _logger.LogWarning("No notifications created for persistence. RoomId={RoomId}",expense.RoomId);
                    return;
                }

                await uow.Repository<PushNotification>().AddRangeAsync(notifications);

                await uow.SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while sending expense notification. ExpenseId={ExpenseId}, RoomId={RoomId}",expense.ExpenseId, expense.RoomId);
            }
        }
        #endregion
    }
}