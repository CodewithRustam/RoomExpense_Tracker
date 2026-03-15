using Domain.Entities;
using Infrastructure;
using Services.ViewModels;

namespace Services.Management
{
    public class SettlementService : ISettlementServices
    {
        private readonly IUnitOfWork _uow;

        private readonly IMemberRepository _memberRepo;
        private readonly IExpenseRepository _expenseRepo;
        private readonly ISettlementRepository _settlementRepo;
        private readonly IRoomRepository _roomRepo;
        private readonly ICurrentUserService currentUser;
        private readonly IEmailSender _emailSender;
        private readonly IMemoryCache _cache;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;

        public SettlementService(IMemberRepository memberRepo, IExpenseRepository expenseRepo,ISettlementRepository settlementRepo, IRoomRepository roomRepo, IEmailSender emailSender, IMemoryCache cache, ICurrentUserService _currentUser, UserManager<ApplicationUser> userManager, AppDbContext context, IUnitOfWork uow)
        {
            _memberRepo = memberRepo;
            _expenseRepo = expenseRepo;
            _settlementRepo = settlementRepo;
            _roomRepo = roomRepo;
            _emailSender = emailSender;
            _cache = cache;
            currentUser = _currentUser;
            _userManager = userManager;
            _context = context;
            _uow = uow;
        }

        public async Task<(bool Success, string Message)> SettleExpenseAsync(SettlementRequest request)
        {
            try
            {
                if (request.RoomId <= 0)
                    return (false, "Invalid room ID.");

                if (string.IsNullOrWhiteSpace(request.PayerName))
                    return (false, "Invalid payer name.");

                if (string.IsNullOrWhiteSpace(request.ReceiverName))
                    return (false, "Invalid receiver name.");

                if (request.SettlementAmount <= 0)
                    return (false, "Settlement amount must be greater than zero.");

                var userId = currentUser.UserId;
                var roomId = request.RoomId;
                var payerName = request.PayerName;
                var receiverName = request.ReceiverName;
                var settlementMonth = request.SettlementMonth ?? DateTime.UtcNow;

                if (!await _roomRepo.AnyAsync(r => r.RoomId == roomId))
                    return (false, "Room not found.");

                var payer = await _memberRepo.GetLoggedInMemberDetails(roomId, payerName, userId);
                if (payer == null)
                    return (false, "Payer not found.");

                var receiver = await _memberRepo.GetRecipientMemberDetails(roomId, receiverName);
                if (receiver == null)
                    return (false, "Receiver not found.");

                if (payer.MemberId == receiver.MemberId)
                    return (false, "You cannot settle with yourself.");

                var monthStart = new DateTime(settlementMonth.Year, settlementMonth.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddTicks(-1);

                var memberExpenses = await _expenseRepo.GetExpensesForMembers(roomId, payer.MemberId, receiver.MemberId, monthStart, monthEnd);
                var memberSettlements = await _settlementRepo.GetSettlementsForMembers(roomId, payer.MemberId, receiver.MemberId, monthStart, monthEnd);


                decimal payerTotalExpenses = memberExpenses.Where(e => e.MemberId == payer.MemberId).Sum(e => e.Amount);
                decimal payerTotalPaid = memberSettlements.Where(s => s.MemberId == payer.MemberId).Sum(s => s.Amount);
                decimal payerTotalReceived = memberSettlements.Where(s => s.PaidToMemberId == payer.MemberId).Sum(s => s.Amount);

                decimal receiverTotalExpenses = memberExpenses.Where(e => e.MemberId == receiver.MemberId).Sum(e => e.Amount);
                decimal receiverTotalPaid = memberSettlements.Where(s => s.MemberId == receiver.MemberId).Sum(s => s.Amount);
                decimal receiverTotalReceived = memberSettlements.Where(s => s.PaidToMemberId == receiver.MemberId).Sum(s => s.Amount);

                decimal totalRoomExpenses = await _expenseRepo.GetTotalRoomExpenses(roomId, monthStart, monthEnd);
                int totalMembers = await _memberRepo.GetMemberCount(roomId);

                if (totalMembers <= 0)
                    return (false, "No members found in this room.");

                decimal avgExpensePerMember = Math.Round(totalRoomExpenses / totalMembers,2);

                decimal payerNetBalance = payerTotalExpenses + payerTotalPaid - payerTotalReceived;
                decimal receiverNetBalance = receiverTotalExpenses + receiverTotalPaid - receiverTotalReceived;

                decimal payerOwes = Math.Round(avgExpensePerMember - payerNetBalance, 2);
                decimal receiverIsOwed = Math.Round(receiverNetBalance - avgExpensePerMember, 2);

                if (payerOwes <= 0)
                    return (false, "You do not owe any amount.");

                if (receiverIsOwed <= 0)
                    return (false, "Receiver is not owed any amount.");

                decimal allowedMaxSettlement = Math.Min(payerOwes, receiverIsOwed);
                bool isSettlementAmountExceed = Math.Truncate(request.SettlementAmount) > Math.Truncate(allowedMaxSettlement);
                if (isSettlementAmountExceed)
                    return (false, $"Settlement cannot exceed ₹{allowedMaxSettlement:F2}");

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
                //await transaction.CommitAsync();

                string cacheKey = CacheHelper.GetCacheKey(roomId, settlementMonth);
                _cache.Remove(cacheKey);
                string roomuserCacheKey = CacheHelper.GetRoomsUserKey(userId);
                _cache.Remove(roomuserCacheKey);
                string monthlyTrendsCacheKey = CacheHelper.GetMonthlyExpenseTrendKey(roomId, settlementMonth);
                _cache.Remove(monthlyTrendsCacheKey);
                string userExpensecacheKey = CacheHelper.GetUserExpensesKey(userId, settlementMonth);
                _cache.Remove(userExpensecacheKey);
                string settlementCacheKey = CacheHelper.GetSettlementCacheKey(roomId, payer.MemberId, settlementMonth);
                _cache.Remove(settlementCacheKey);
                string settlCacheKey = CacheHelper.GetSettlementCacheKey(roomId, receiver.MemberId, settlementMonth);
                _cache.Remove(settlCacheKey);

                foreach (var isMemberInclude in new[] { true, false })
                {
                    string expenseDetails = CacheHelper.GetMonthlyExpensesKey(roomId, settlementMonth, isMemberInclude);
                    _cache.Remove(expenseDetails);
                }

                if (newSettlement.SettlementId > 0)
                {
                    var payerUser = await _userManager.FindByIdAsync(payer.ApplicationUserId!);
                    var receiverUser = await _userManager.FindByIdAsync(receiver.ApplicationUserId!);

                    SettlementEmailVM settlementEmailVM = new SettlementEmailVM()
                    {
                        PayerEmail = payerUser!.Email,
                        PayerUserName = payerUser!.UserName,
                        PayerName = payer.Name,

                        ReceiverEmail = receiverUser!.Email,
                        ReceiverUserName = receiverUser!.UserName,
                        ReceiverName = receiver.Name,

                        Amount = request.SettlementAmount,
                        SettlementForMonth = settlementMonth,
                        RoomId = roomId
                    };
                    _ = Task.Run(async () =>
                    {
                        await SendSettlementEmailAsync(settlementEmailVM);
                    });
                    return (true, $"Successfully settled ₹{request.SettlementAmount:F2} with {receiverName}.");
                }
                return (true, $"Settlement failed ₹{request.SettlementAmount:F2} with {receiverName}.");
            }
            catch (Exception)
            {
                throw new NotFoundException("An unexpected error occurred while settling expenses. Please try again.");
            }
        }
        public async Task SendSettlementEmailAsync(SettlementEmailVM settlementEmailVM)
        {
            //if (payer == null || receiver == null || string.IsNullOrWhiteSpace(payer.ApplicationUserId) || string.IsNullOrWhiteSpace(receiver.ApplicationUserId))
            //{
            //    //_logger.LogWarning("SendSettlementEmailAsync skipped: payer or receiver info missing.");
            //    return;
            //}

            if (!string.IsNullOrWhiteSpace(settlementEmailVM.ReceiverEmail))
            {
                string receiverEmailSubject = $"Settlement Received - {settlementEmailVM.SettlementForMonth:MMMM yyyy}";
                string receiverEmailBody = EmailTemplates.GetSettlementEmailTemplate(
                    settlementEmailVM.ReceiverUserName!,
                    $"{settlementEmailVM.PayerName} has settled ₹{settlementEmailVM.Amount:F2} with you for {settlementEmailVM.SettlementForMonth:MMMM yyyy}.",
                    "Settlement Received", settlementEmailVM.RoomId
                );

                await _emailSender.SendEmailAsync(settlementEmailVM.ReceiverEmail, receiverEmailSubject, receiverEmailBody);
            }

            if (!string.IsNullOrWhiteSpace(settlementEmailVM.PayerEmail))
            {
                string payerEmailSubject = $"Settlement Paid - {settlementEmailVM.SettlementForMonth:MMMM yyyy}";
                string payerEmailBody = EmailTemplates.GetSettlementEmailTemplate(
                    settlementEmailVM.PayerUserName!,
                    $"You have successfully settled ₹{settlementEmailVM.Amount:F2} to {settlementEmailVM.ReceiverName} for {settlementEmailVM.SettlementForMonth:MMMM yyyy}.",
                    "Settlement Paid", settlementEmailVM.RoomId
                );
                await _emailSender.SendEmailAsync(settlementEmailVM.PayerEmail, payerEmailSubject, payerEmailBody);
            }
        }
    }
}
