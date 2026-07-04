namespace Services.Management
{
    public class SettlementCalculatorService : ISettlementCalculatorService
    {
        public List<SettlementDetail> ComputeSettlementsForMember(Dictionary<int, decimal> balancesDict, IReadOnlyList<Member> members, int targetMemberId)
        {
            var settlements = new List<SettlementDetail>();

            if (!balancesDict.TryGetValue(targetMemberId, out decimal targetBalance) || Math.Abs(targetBalance) < 0.5m)
                return settlements;

            var creditors = new List<(int Id, decimal Balance, string Name)>();
            var debtors = new List<(int Id, decimal Balance, string Name)>();

            foreach (var member in members)
            {
                if (member.MemberId == targetMemberId) continue;

                var balance = balancesDict.GetValueOrDefault(member.MemberId, 0m);
                if (balance > 0) creditors.Add((member.MemberId, balance, member.Name));
                else if (balance < 0) debtors.Add((member.MemberId, Math.Abs(balance), member.Name));
            }

            if (creditors.Count > 0) creditors.Sort((a, b) => b.Balance.CompareTo(a.Balance));
            if (debtors.Count > 0) debtors.Sort((a, b) => b.Balance.CompareTo(a.Balance));

            if (targetBalance < 0)
            {
                var amountToPay = Math.Abs(targetBalance);

                amountToPay = ProcessSettlements(amountToPay, creditors, settlements);
                if (amountToPay > 0)
                {
                    ProcessSettlements(amountToPay, debtors, settlements);
                }
            }

            return settlements;
        }

        private decimal ProcessSettlements(decimal amountToPay, List<(int Id, decimal Balance, string Name)> targets, List<SettlementDetail> settlements)
        {
            int i = 0;
            while (amountToPay > 0 && i < targets.Count)
            {
                var settleAmount = Math.Min(amountToPay, targets[i].Balance);
                if (settleAmount > 0)
                {
                    settlements.Add(new SettlementDetail
                    {
                        ToMemberId = targets[i].Id,
                        ToMemberName = targets[i].Name,
                        Amount = settleAmount
                    });
                    amountToPay -= settleAmount;
                }
                i++;
            }
            return amountToPay;
        }

        public List<MemberExpenseSummary> CalculateMemberExpenseSummary(IReadOnlyList<ExpenseRecordDto> expenses, IReadOnlyList<Settlement> settlements, IReadOnlyList<Member> members, string? currentUserId)
        {
            var summaries = new List<MemberExpenseSummary>();
            if (members == null || !members.Any()) return summaries;

            var totalExpenses = expenses.Sum(e => e.Amount);
            var avgShare = members.Count > 0 ? Math.Round(totalExpenses / members.Count, 2) : 0m;

            foreach (var member in members)
            {
                var totalMemberExpense = expenses.Where(e => e.PayerId == member.MemberId).Sum(e => e.Amount);
                var amountPaid = settlements.Where(s => s.MemberId == member.MemberId).Sum(s => s.Amount);
                var amountReceived = settlements.Where(s => s.PaidToMemberId == member.MemberId).Sum(s => s.Amount);

                decimal netBalance = (totalMemberExpense + amountPaid) - amountReceived - avgShare;

                bool isSettled = Math.Abs(netBalance) < 0.5m;

                summaries.Add(new MemberExpenseSummary
                {
                    MemberId = member.MemberId,
                    MemberName = member.Name,
                    TotalMemberExpense = totalMemberExpense,
                    AmountPaid = amountPaid,
                    AmountReceived = amountReceived,
                    NetBalance = isSettled ? 0 : netBalance,
                    BadgeText = isSettled ? "Settled up" : (netBalance > 0 ? "Owed" : "Owe"),
                    BadgeAmount = isSettled ? 0 : Math.Abs(netBalance),
                    IsSettleShow = member.ApplicationUserId == currentUserId
                });
            }

            return summaries;
        }
    }
}