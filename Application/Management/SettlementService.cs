using Domain.AppUser;
using Domain.Entities;
using Domain.Interfaces;
using ExpenseTrakcerHepler;
using Infrastructure.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Services.Interfaces;

namespace Services.Management
{
    public class SettlementService : ISettlementServices
    {
        private readonly IMemberRepository _memberRepo;
        private readonly IExpenseRepository _expenseRepo;
        private readonly ISettlementRepository _settlementRepo;
        private readonly IRoomRepository _roomRepo;
        private readonly ICurrentUserService currentUser;
        private readonly IEmailSender _emailSender;
        private readonly IMemoryCache _cache;
        private readonly UserManager<ApplicationUser> _userManager;


        public SettlementService(IMemberRepository memberRepo, IExpenseRepository expenseRepo,ISettlementRepository settlementRepo, IRoomRepository roomRepo, IEmailSender emailSender, IMemoryCache cache, ICurrentUserService _currentUser, UserManager<ApplicationUser> userManager)
        {
            _memberRepo = memberRepo;
            _expenseRepo = expenseRepo;
            _settlementRepo = settlementRepo;
            _roomRepo = roomRepo;
            _emailSender = emailSender;
            _cache = cache;
            currentUser = _currentUser;
            _userManager = userManager;
        }

        public async Task<(bool Success, string Message)> SettleExpenseAsync(int roomId, string memberName, string paidToMemberName, decimal amount, DateTime settlementForMonth)
        {
            try
            {
                var userId = currentUser.UserId;
                var member = await _memberRepo.GetLoggedInMemberDetails(roomId, memberName,userId);
                if (member == null) return (false, "Member not found.");

                var paidToMember = await _memberRepo.GetRecipientMemberDetails(roomId, paidToMemberName);
                if (paidToMember == null) return (false, "Recipient not found.");

                if (!await _roomRepo.IsValidRoomAsync(roomId)) return (false, "Room not found.");

                var start = settlementForMonth;
                var end = settlementForMonth.AddMonths(1).AddTicks(-1);

                decimal loggedInExpenses = await _expenseRepo.GetMemberTotalExpenses(roomId, member.MemberId, start, end);
                decimal settlementsPaid = await _settlementRepo.GetSettlementsPaid(roomId, member.MemberId, start, end);
                decimal settlementsReceived = await _settlementRepo.GetSettlementsReceived(roomId, member.MemberId, start, end);

                decimal paidToExpenses = await _expenseRepo.GetMemberTotalExpenses(roomId, paidToMember.MemberId, start, end);
                decimal paidToSettlementsPaid = await _settlementRepo.GetSettlementsPaid(roomId, paidToMember.MemberId, start, end);
                decimal paidToSettlementsReceived = await _settlementRepo.GetSettlementsReceived(roomId, paidToMember.MemberId, start, end);

                decimal totalRoomExpenses = await _expenseRepo.GetTotalRoomExpenses(roomId, start, end);
                int memberCount = await _memberRepo.GetMemberCount(roomId);

                var avgPerPerson = memberCount > 0 ? totalRoomExpenses / memberCount : 0;

                var payerBalance = loggedInExpenses + settlementsPaid + settlementsReceived;
                var payerOwedAmount = Math.Abs(payerBalance - avgPerPerson);

                var recipientBalance = paidToExpenses - paidToSettlementsPaid + paidToSettlementsReceived;
                var recipientOwedAmount = recipientBalance - avgPerPerson;

                if (payerBalance >= avgPerPerson) return (false, "You are not owing any amount.");
                if (recipientBalance <= avgPerPerson) return (false, "Recipient is not owed any amount.");

                var maxSettlement = Math.Max(Math.Round(payerOwedAmount), Math.Round(recipientOwedAmount));
                if (amount > maxSettlement) return (false, $"Settlement cannot exceed ₹{maxSettlement:F2}");

                var settlement = new Settlement
                {
                    MemberId = member.MemberId,
                    PaidToMemberId = paidToMember.MemberId,
                    RoomId = roomId,
                    Amount = amount,
                    SettlementDate = DateTime.Now,
                    SettlementForDate = settlementForMonth
                };

                await _settlementRepo.AddSettlement(settlement);

                if (settlement.SettlementId > 0)
                {
                    string cacheKey = CacheHepler.GetCacheKey(roomId, settlementForMonth);
                    _cache.Remove(cacheKey);

                    await SendSettlementEmailAsync(member, paidToMember, amount, settlementForMonth);
                    return (true, $"Successfully settled ₹{amount} with {paidToMemberName}.");
                }
            }
            catch (Exception)
            {
                throw;
            }
            return (false, "Settlement failed.");
        }
        public async Task SendSettlementEmailAsync(Member member, Member paidToMember, decimal amount, DateTime settlementForMonth)
        {
            try
            {
                if (member != null && paidToMember != null && member.ApplicationUserId != null && paidToMember.ApplicationUserId != null)
                {
                    var payerUser = await _userManager.FindByIdAsync(member.ApplicationUserId);
                    var receiverUser = await _userManager.FindByIdAsync(paidToMember.ApplicationUserId);

                    if (receiverUser != null && receiverUser.Email != null)
                    {
                        await _emailSender.SendEmailAsync(
                            receiverUser.Email,
                            $"Settlement Received - {DateTime.Today:MMMM yyyy}",
                            $"Hi {receiverUser.UserName},<br/><br/>{member.Name} has settled ₹{amount} with you for {settlementForMonth:MMMM yyyy}.<br/><br/>Regards,<br/>Expense Tracker"
                        );
                    }

                    if (payerUser != null && payerUser.Email != null)
                    {
                        await _emailSender.SendEmailAsync(
                            payerUser.Email,
                            $"Settlement Paid - {DateTime.Today:MMMM yyyy}",
                            $"Hi {payerUser.UserName},<br/><br/>You have successfully settled ₹{amount} to {paidToMember.Name} for {settlementForMonth:MMMM yyyy}.<br/><br/>Regards,<br/>Expense Tracker"
                        );
                    }
                }
            }
            catch (Exception)
            {

                throw;
            }
        }
    }
}
