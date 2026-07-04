namespace Services.Interfaces
{
    public interface ISettlementCalculatorService
    {
        List<SettlementDetail> ComputeSettlementsForMember(Dictionary<int, decimal> balancesDict, IReadOnlyList<Member> members, int targetMemberId);

        List<MemberExpenseSummary> CalculateMemberExpenseSummary(IReadOnlyList<ExpenseRecordDto> expenses, IReadOnlyList<Settlement> settlements, IReadOnlyList<Member> members, string? currentUserId);
    }
}