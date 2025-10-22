using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Dto
{
    public class MemberExpensesDto
    {
        public int MemberId { get; set; }
        public string Name { get; set; } = "";
        public decimal TotalExpense { get; set; }
    }
    public class CategoryExpenseDto
    {
        public string Category { get; set; } = "";
        public decimal TotalAmount { get; set; }
    }
}
