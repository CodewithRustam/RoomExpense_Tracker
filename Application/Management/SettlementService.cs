namespace Services.Management
{
    public class SettlementService : ISettlementServices
    {
        private readonly IUnitOfWork _uow;
        private readonly IMemberRepository _memberRepo;
        private readonly IExpenseRepository _expenseRepo;
        private readonly ISettlementRepository _settlementRepo;
        private readonly IRoomRepository _roomRepo;

        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<SettlementService> _logger;
        private readonly ISettlementCacheService _cacheService;
        private readonly ISettlementNotificationService _notificationService;
        private readonly ISettlementCalculatorService _calculatorService;

        public SettlementService(
            IMemberRepository memberRepo,
            IExpenseRepository expenseRepo,
            ISettlementRepository settlementRepo,
            IRoomRepository roomRepo,
            ICurrentUserService currentUser,
            ILogger<SettlementService> logger,
            ISettlementCacheService cacheService,
            ISettlementNotificationService notificationService,
            ISettlementCalculatorService calculatorService,
            IUnitOfWork uow)
        {
            _memberRepo = memberRepo;
            _expenseRepo = expenseRepo;
            _settlementRepo = settlementRepo;
            _roomRepo = roomRepo;
            _currentUser = currentUser;
            _logger = logger;
            _cacheService = cacheService;
            _notificationService = notificationService;
            _calculatorService = calculatorService;
            _uow = uow;
        }

        public async Task<(bool Success, string Message)> SettleExpenseAsync(SettlementRequest request)
        {
            try
            {
                // 1. Fast, memory-only validation
                if (request.RoomId <= 0) return (false, SettlementMessages.InvalidRoomId);
                if (string.IsNullOrWhiteSpace(request.PayerName)) return (false, SettlementMessages.InvalidPayer);
                if (string.IsNullOrWhiteSpace(request.ReceiverName)) return (false, SettlementMessages.InvalidReceiver);
                if (request.SettlementAmount <= 0) return (false, SettlementMessages.InvalidAmount);

                var userId = _currentUser.UserId;
                var roomId = request.RoomId;
                var settlementMonth = request.SettlementMonth ?? DateTime.UtcNow;

                // 2. Fetch payer and receiver SEQUENTIALLY to prevent DbContext threading crashes
                var payer = await _memberRepo.GetLoggedInMemberDetails(roomId, request.PayerName, userId);
                var receiver = await _memberRepo.GetRecipientMemberDetails(roomId, request.ReceiverName);

                // Note: We removed the Room Exists check because if members exist, the room exists.
                if (payer == null) return (false, SettlementMessages.PayerNotFound);
                if (receiver == null) return (false, SettlementMessages.ReceiverNotFound);
                if (payer.MemberId == receiver.MemberId) return (false, SettlementMessages.SelfSettlement);

                // 3. Fetch monthly expenses, settlements, and room members
                var expenses = await _expenseRepo.GetMonthlyExpenses(roomId, settlementMonth);
                var settlements = await _settlementRepo.GetMonthlySettlements(roomId, settlementMonth);
                var allMembers = await _memberRepo.GetMembersByRoomId(roomId, userId);

                if (allMembers.Count <= 0) return (false, SettlementMessages.NoMembers);

                var startOfMonth = new DateTime(settlementMonth.Year, settlementMonth.Month, 1);
                var endOfMonth = startOfMonth.AddMonths(1);

                var monthMembers = allMembers.Where(m =>
                    (m.JoinedDate.Date < endOfMonth.Date && (m.LeftDate == null || m.LeftDate.Value.Date >= startOfMonth.Date)) ||
                    expenses.Any(e => e.PayerId == m.MemberId || (e.Splits != null && e.Splits.Any(s => s.MemberId == m.MemberId))) ||
                    settlements.Any(s => s.MemberId == m.MemberId || s.PaidToMemberId == m.MemberId)
                ).ToList();

                // 4. Calculate member expense summary using split ledger and joined dates
                var memberSummaries = _calculatorService.CalculateMemberExpenseSummary(
                    expenses,
                    settlements.ToList(),
                    monthMembers,
                    userId);

                var payerSummary = memberSummaries.FirstOrDefault(m => m.MemberId == payer.MemberId);
                var receiverSummary = memberSummaries.FirstOrDefault(m => m.MemberId == receiver.MemberId);

                if (payerSummary == null || payerSummary.NetBalance >= 0)
                    return (false, SettlementMessages.PayerDoesNotOwe);

                if (receiverSummary == null || receiverSummary.NetBalance <= 0)
                    return (false, SettlementMessages.ReceiverNotOwed);

                decimal payerOwes = Math.Abs(payerSummary.NetBalance);
                decimal receiverIsOwed = receiverSummary.NetBalance;

                // 5. Direct comparison with a 1-cent grace buffer to avoid floating point strictness
                decimal allowedMaxSettlement = Math.Min(payerOwes, receiverIsOwed);

                if (request.SettlementAmount > allowedMaxSettlement + 0.01m)
                    return (false, SettlementMessages.ExceedsMax(allowedMaxSettlement));

                // 6. Persistence
                var newSettlement = new Settlement
                {
                    MemberId = payer.MemberId,
                    PaidToMemberId = receiver.MemberId,
                    RoomId = roomId,
                    Amount = request.SettlementAmount,
                    SettlementDate = DateTimeProvider.NowIST,
                    SettlementForDate = settlementMonth
                };

                await _settlementRepo.AddAsync(newSettlement);
                await _uow.SaveAsync();

                if (newSettlement.SettlementId > 0)
                {
                    _cacheService.ClearCachesAfterSettlement(roomId, payer.MemberId, receiver.MemberId, userId!, settlementMonth);

                    _notificationService.FireAndForgetSettlementEmail(
                        roomId,
                        payer.ApplicationUserId!,
                        payer.Name,
                        receiver.ApplicationUserId!,
                        receiver.Name,
                        request.SettlementAmount,
                        settlementMonth);

                    return (true, SettlementMessages.Success(request.SettlementAmount, request.ReceiverName));
                }

                return (false, SettlementMessages.Failed(request.SettlementAmount, request.ReceiverName));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to settle expense for RoomId: {RoomId}", request.RoomId);
                return (false, SettlementMessages.UnexpectedError);
            }
        }
    }
}