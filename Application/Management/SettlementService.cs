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

        public SettlementService(
            IMemberRepository memberRepo,
            IExpenseRepository expenseRepo,
            ISettlementRepository settlementRepo,
            IRoomRepository roomRepo,
            ICurrentUserService currentUser,
            ILogger<SettlementService> logger,
            ISettlementCacheService cacheService,
            ISettlementNotificationService notificationService,
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

                // Date calculations
                var monthStart = new DateTime(settlementMonth.Year, settlementMonth.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddTicks(-1); // Keep if repo strictly relies on <=

                // 3. Fetch all required math data SEQUENTIALLY to prevent DbContext threading crashes
                var memberExpenses = await _expenseRepo.GetExpensesForMembers(roomId, payer.MemberId, receiver.MemberId, monthStart, monthEnd);
                var memberSettlements = await _settlementRepo.GetSettlementsForMembers(roomId, payer.MemberId, receiver.MemberId, monthStart, monthEnd);
                decimal totalRoomExpenses = await _expenseRepo.GetTotalRoomExpenses(roomId, monthStart, monthEnd);
                int totalMembers = await _memberRepo.GetMemberCountAsync(roomId);

                if (totalMembers <= 0) return (false, SettlementMessages.NoMembers);

                // 4. Local Math Processing
                decimal payerTotalExpenses = memberExpenses.Where(e => e.MemberId == payer.MemberId).Sum(e => e.Amount);
                decimal payerTotalPaid = memberSettlements.Where(s => s.MemberId == payer.MemberId).Sum(s => s.Amount);
                decimal payerTotalReceived = memberSettlements.Where(s => s.PaidToMemberId == payer.MemberId).Sum(s => s.Amount);

                decimal receiverTotalExpenses = memberExpenses.Where(e => e.MemberId == receiver.MemberId).Sum(e => e.Amount);
                decimal receiverTotalPaid = memberSettlements.Where(s => s.MemberId == receiver.MemberId).Sum(s => s.Amount);
                decimal receiverTotalReceived = memberSettlements.Where(s => s.PaidToMemberId == receiver.MemberId).Sum(s => s.Amount);

                decimal avgExpensePerMember = Math.Round(totalRoomExpenses / totalMembers, 2);

                decimal payerNetBalance = payerTotalExpenses + payerTotalPaid - payerTotalReceived;
                decimal receiverNetBalance = receiverTotalExpenses + receiverTotalPaid - receiverTotalReceived;

                decimal payerOwes = Math.Round(avgExpensePerMember - payerNetBalance, 2);
                decimal receiverIsOwed = Math.Round(receiverNetBalance - avgExpensePerMember, 2);

                if (payerOwes <= 0) return (false, SettlementMessages.PayerDoesNotOwe);
                if (receiverIsOwed <= 0) return (false, SettlementMessages.ReceiverNotOwed);

                // 5. Bug Fix: Direct comparison with a 1-cent grace buffer to avoid floating point strictness
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