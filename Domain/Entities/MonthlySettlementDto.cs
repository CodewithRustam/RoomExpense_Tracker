using System;namespace Domain.Entities
{
    public class MonthlySettlementDto
    {
        public int MemberId { get; set; }
        public string Name { get; set; } = "";
        public decimal NetBalance { get; set; }
    }
}
