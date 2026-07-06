namespace Services.Management
{
    public class ExpenseService : IExpenseServices
    {
        private readonly IUnitOfWork _uow;

        private readonly IExpenseRepository _expenseRepo;
        private readonly IMemberRepository _memberRepo;
        private readonly ISettlementRepository _settlementRepo;
        private readonly IRoomRepository _roomRepo;

        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<ExpenseService> _logger;

        private readonly ISettlementCalculatorService _calculatorService;
        private readonly IExpenseCacheService _cacheService;
        private readonly IRateLimitService _rateLimitService;
        private readonly IExpenseNotificationService _notificationService;
        private readonly IExpenseMapper _mapper;

        public ExpenseService(
            IUnitOfWork uow,
            IExpenseRepository expenseRepo,
            IMemberRepository memberRepo,
            ISettlementRepository settlementRepo,
            IRoomRepository roomRepo,
            ICurrentUserService currentUser,
            ILogger<ExpenseService> logger,
            ISettlementCalculatorService calculatorService,
            IExpenseCacheService cacheService,
            IRateLimitService rateLimitService,
            IExpenseNotificationService notificationService,
            IExpenseMapper mapper)
        {
            _uow = uow;
            _expenseRepo = expenseRepo;
            _memberRepo = memberRepo;
            _settlementRepo = settlementRepo;
            _roomRepo = roomRepo;
            _currentUser = currentUser;
            _logger = logger;
            _calculatorService = calculatorService;
            _cacheService = cacheService;
            _rateLimitService = rateLimitService;
            _notificationService = notificationService;
            _mapper = mapper;
        }

        public async Task<ApiResponse> AddExpenses(ExpenseViewModel model)
        {
            var userId = _currentUser.UserId!;
            int memberId = await _memberRepo.GetMemberIdAsync(userId, model.RoomId);

            if (memberId == 0)
            {
                _logger.LogWarning("AddExpense failed: Member not found for UserId: {UserId}, RoomId: {RoomId}", userId, model.RoomId);
                return ApiResponse.Fail(ExpenseMessages.MemberNotFound);
            }

            if (_rateLimitService.IsRateLimited(userId, 5, TimeSpan.FromMinutes(1)))
                return ApiResponse.Fail(ExpenseMessages.RateLimitExceeded);

            var expense = _mapper.MapToExpense(model, memberId);

            if (await _expenseRepo.IsExpenseExist(expense))
                return ApiResponse.Fail(ExpenseMessages.ExpenseExists);

            await _expenseRepo.AddAsync(expense);

            if (await _uow.SaveAsync() <= 0)
            {
                _logger.LogError("Database commit failed during AddExpense for UserId: {UserId}", userId);
                return ApiResponse.Fail(ExpenseMessages.SaveFailed);
            }

            _cacheService.ClearCachesOnAddOrUpdate(model.RoomId, model.Date, userId, memberId);
            _notificationService.FireAndForgetExpenseNotification(expense, userId, _currentUser.UserName!);

            return ApiResponse.SuccessRes(ExpenseMessages.SuccessAdd);
        }

        public async Task<ApiResponse> UpdateExpenses(ExpenseViewModel model)
        {
            if (await _settlementRepo.IsMonthSettledForRoomAsync(model.RoomId, model.Date))
                return ApiResponse.Fail(ExpenseMessages.MonthSettled);

            var expense = _mapper.MapToExpense(model, model.MemberId);
            expense.ExpenseId = model.ExpenseId ?? 0;

            if (!await _expenseRepo.IsExpenseExistForUser(expense))
                return ApiResponse.Fail(ExpenseMessages.ExpenseNotFound);

            _expenseRepo.Update(expense);

            if (await _uow.SaveAsync() <= 0)
                return ApiResponse.Fail(ExpenseMessages.UpdateFailed);

            _cacheService.ClearCachesOnAddOrUpdate(model.RoomId, model.Date, _currentUser.UserId!, model.MemberId);
            _notificationService.FireAndForgetExpenseNotification(expense, _currentUser.UserId!, _currentUser.UserName!, isUpdate: true);

            return ApiResponse.SuccessRes(ExpenseMessages.SuccessUpdate);
        }

        public async Task<ApiResponse> DeleteExpense(int expenseId)
        {
            var userId = _currentUser.UserId!;

            if (_rateLimitService.ReachedDailyDeleteLimit(userId))
                return ApiResponse.Fail(ExpenseMessages.DailyDeleteLimit);

            var expense = await _expenseRepo.GetByIdAsync(expenseId);
            if (expense == null)
                return ApiResponse.Fail(ExpenseMessages.ExpenseNotFound);

            if (await _settlementRepo.IsMonthSettledForRoomAsync(expense.RoomId, expense.Date))
                return ApiResponse.Fail(ExpenseMessages.MonthSettled);

            expense.IsDeleted = true;
            _expenseRepo.Update(expense);

            if (await _uow.SaveAsync() <= 0)
                return ApiResponse.Fail(ExpenseMessages.DeleteFailed);

            _rateLimitService.IncrementDailyDeleteLimit(userId);
            _cacheService.ClearCachesOnDelete(expense.RoomId, expense.Date, userId);

            return ApiResponse.SuccessRes(ExpenseMessages.SuccessDelete);
        }

        public async Task<IReadOnlyList<string>> GetExpenseMonthsByUserId()
        {
            return await _expenseRepo.GetExpenseMonthsByUserId(_currentUser.UserId!);
        }

        public async Task<ApiResponse> GetRoomExpensesForApi(int roomId, string? monthReq, bool includeRoomInfo = true)
        {
            DateTime currentExpenseMonth;
            if (string.IsNullOrEmpty(monthReq))
            {
                var allExpenses = await _expenseRepo.GetAllAsync(x => x.RoomId == roomId);
                currentExpenseMonth = allExpenses.Count > 0 ? allExpenses.Max(x => x.Date) : DateTime.UtcNow;
            }
            else if (!DateTimeParser.ParseMonthYear(monthReq, out currentExpenseMonth))
            {
                return ApiResponse.Fail(ExpenseMessages.InvalidMonthFormat);
            }

            var expenses = await _expenseRepo.GetMonthlyExpenses(roomId, currentExpenseMonth);
            var settlements = await _settlementRepo.GetMonthlySettlements(roomId, currentExpenseMonth);
            var members = await _memberRepo.GetMembersByRoomId(roomId, _currentUser.UserId);
            var isSettled = await _settlementRepo.IsMonthSettledForRoomAsync(roomId, currentExpenseMonth);

            var response = new RoomExpenseResponse
            {
                RoomId = roomId,
                TotalMontlyExpense = expenses.Sum(e => e.Amount),
                SelectedMonth = $"{currentExpenseMonth.Year:D4}-{currentExpenseMonth.Month:D2}",
                Expenses = _mapper.MapToExpenseDetailResponses(expenses, _currentUser.UserId!, isSettled)
            };

            response.MembersSummary = _calculatorService.CalculateMemberExpenseSummary(
                expenses,
                settlements.ToList(),
                members,
                _currentUser.UserId);

            if (includeRoomInfo && members.Count > 0)
            {
                response.RoomName = (await _roomRepo.GetRoomNameAsync(roomId)) ?? string.Empty;
                response.AvailableMonths = (await _expenseRepo.GetExpenseMonths(roomId)).ToList();
            }

            return ApiResponse<RoomExpenseResponse>.SuccessRes(response, ExpenseMessages.SuccessFetch);
        }
        public async Task<ApiResponse> GetMonthlyExpensesTrend(int roomId, string month)
        {
            if (!DateTimeParser.ParseMonthYear(month, out var targetMonth))
                return ApiResponse.Fail(ExpenseMessages.InvalidMonthFormat);

            if (_cacheService.TryGetMonthlyTrend(roomId, targetMonth, out var cachedResponse))
                return ApiResponse<MonthlyExpensesTrendResponse>.SuccessRes(cachedResponse!, ExpenseMessages.SuccessFetch);

            var (memberDtos, categoryDtos, topSpendsDtos) = await _expenseRepo.GetMonthlyExpensesTrendAsync(roomId, targetMonth);

            if (memberDtos.Count == 0 && categoryDtos.Count == 0 && topSpendsDtos.Count == 0)
                return ApiResponse.Fail(ExpenseMessages.NoTrendData);

            // This mapping could also be moved to the IExpenseMapper for strict purity
            var response = new MonthlyExpensesTrendResponse
            {
                Months = new List<string> { targetMonth.ToString("MMMM yyyy") },
                Members = memberDtos.Select(m => new MemberExpenses { Name = m.Name, MonthlyExpenses = new List<decimal> { m.TotalExpense } }).ToList(),
                CategoryExpenses = categoryDtos.Select(c => new CategoryMonthlyExpense { CategoryName = c.Category, MonthlyTotals = new List<decimal> { c.TotalAmount }, IconName = CategoryMapper.GetIconForCategory(c.Category) }).ToList(),
                TopSpends = topSpendsDtos.Select(t => new TopSpend { CategoryName = t.Category, MonthlyTotals = new List<decimal> { t.TotalAmount }, TotalAmount = t.TotalAmount, IconName = CategoryMapper.GetIconForCategory(t.Category) }).ToList()
            };

            _cacheService.SetMonthlyTrend(roomId, targetMonth, response);
            return ApiResponse<MonthlyExpensesTrendResponse>.SuccessRes(response, ExpenseMessages.SuccessFetch);
        }

        public async Task<ApiResponse> GetSettlementDetails(int roomId, int memberId, string month)
        {
            if (!DateTimeParser.ParseMonthYear(month, out var targetMonth))
                return ApiResponse.Fail(ExpenseMessages.InvalidMonthFormat);

            if (roomId <= 0 || !await _roomRepo.IsValidRoomAsync(roomId))
                return ApiResponse.Fail(ExpenseMessages.InvalidRoomId);

            var members = await _memberRepo.GetMembersByRoomId(roomId, _currentUser.UserId);
            if (members.Count == 0)
                return ApiResponse.Fail(ExpenseMessages.NoMembersFound);

            var monthlyBalances = await _settlementRepo.GetMonthlySettlementsDetails(roomId, targetMonth);
            if (monthlyBalances.Count == 0)
                return ApiResponse.Fail(ExpenseMessages.NoSettlementData);

            var balancesDict = monthlyBalances.ToDictionary(m => m.MemberId, m => m.NetBalance);

            var settlementDetails = _calculatorService.ComputeSettlementsForMember(balancesDict, members, memberId);

            return ApiResponse<SettlementData>.SuccessRes(new SettlementData
            {
                NetBalance = balancesDict.GetValueOrDefault(memberId, 0m),
                Settlements = settlementDetails
            }, ExpenseMessages.SuccessFetch);
        }

        public async Task<ApiResponse> GetUserExpensesForApi(string month)
        {
            var targetMonth = ParseTargetMonth(month);
            var userId = _currentUser.UserId;

            if (string.IsNullOrEmpty(userId))
                return ApiResponse.Fail(ExpenseMessages.MemberNotFound);

            if (_cacheService.TryGetUserExpenses(userId, targetMonth, out var cachedExpenses))
                return ApiResponse<List<UserExpenseResponse>>.SuccessRes(cachedExpenses!, ExpenseMessages.SuccessFetch);

            var expenses = await _expenseRepo.GetUserExpenses(userId, targetMonth);
            if (expenses.Count == 0)
                return ApiResponse.Fail(ExpenseMessages.ExpenseNotFound);

            var response = _mapper.MapToUserExpenseResponses(expenses, userId);

            _cacheService.SetUserExpenses(userId, targetMonth, response);
            return ApiResponse<List<UserExpenseResponse>>.SuccessRes(response, ExpenseMessages.SuccessFetch);
        }

        public async Task<ApiResponse> GetHomeExpenseTrends(int roomId)
        {
            var today = DateTime.UtcNow;
            var startDate = new DateTime(today.AddMonths(-5).Year, today.AddMonths(-5).Month, 1);

            var expenses = await _expenseRepo.GetHomeExpenseTrends(roomId, startDate);

            var trendData = expenses.GroupBy(e => (e.Date.Year, e.Date.Month))
                                    .ToDictionary(g => g.Key, g => g.Sum(e => e.Amount));

            var finalTrend = new List<MonthlyHomeTrendVM>(6);
            for (int i = -5; i <= 0; i++)
            {
                var targetDate = today.AddMonths(i);
                finalTrend.Add(new MonthlyHomeTrendVM
                {
                    MonthName = targetDate.ToString("MMM"),
                    Total = trendData.GetValueOrDefault((targetDate.Year, targetDate.Month), 0m)
                });
            }

            return ApiResponse<List<MonthlyHomeTrendVM>>.SuccessRes(finalTrend, ExpenseMessages.SuccessFetch);
        }

        private DateTime ParseTargetMonth(string? month)
        {
            if (string.IsNullOrEmpty(month)) return new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            if (!DateTimeParser.ParseMonthYear(month, out var targetMonth)) throw new ArgumentException(ExpenseMessages.InvalidMonthFormat);
            return targetMonth;
        }
    }
}