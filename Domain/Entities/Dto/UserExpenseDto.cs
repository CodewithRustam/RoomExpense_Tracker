using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities.Dto
{
    public class UserExpenseDto
    {
        public  string Item { get; set; }
        public  decimal Amount { get; set; }
        public  DateTime ExpenseDate { get; set; }
        public  string? MemberName { get; set; }
        public  string? RoomName { get; set; }
        public  string? Category { get; set; }
    }
}
