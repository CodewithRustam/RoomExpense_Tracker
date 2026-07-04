using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Management
{
    public class RoomMapper : IRoomMapper
    {
        public IReadOnlyList<RoomResponse> MapToRoomResponses(IReadOnlyList<Room> rooms)
        {
            if (rooms == null || !rooms.Any()) return Array.Empty<RoomResponse>();

            return rooms.Select(r =>
            {
                if (r.Expenses == null || !r.Expenses.Any())
                {
                    return new RoomResponse
                    {
                        RoomId = r.RoomId,
                        Name = r.Name ?? string.Empty,
                        CreatedByUserId = r.CreatedByUserId,
                        CreatedDate = r.CreatedDate,
                        MemberNames = string.Join(", ", r.Members.Select(m => m.Name)),
                        TotalAmount = 0,
                        Type = "Private", // Consider moving this to an Enum in the future
                        IconName = string.Empty,
                        Status = r.IsDeleted ? "Deleted" : "Active"
                    };
                }

                var lastExpenseDate = r.Expenses.Max(e => e.Date);

                var totalAmount = r.Expenses
                    .Where(e => e.Date.Month == lastExpenseDate.Month &&
                                e.Date.Year == lastExpenseDate.Year &&
                                (e.IsDeleted == false || e.IsDeleted == null))
                    .Sum(e => e.Amount);

                return new RoomResponse
                {
                    RoomId = r.RoomId,
                    Name = r.Name ?? string.Empty,
                    CreatedByUserId = r.CreatedByUserId,
                    CreatedDate = r.CreatedDate,
                    MemberNames = string.Join(", ", r.Members.Select(m => m.Name)),
                    TotalAmount = totalAmount,
                    Type = "Private",
                    IconName = string.Empty,
                    Status = r.IsDeleted ? "Deleted" : "Active",
                    Month = new DateTime(lastExpenseDate.Year, lastExpenseDate.Month, 1).ToString("yyyy-MMM")
                };
            }).ToList();
        }

        public RoomDetailsViewModel? MapToRoomDetailsViewModel(Room? room, string? month, bool isFromSettled)
        {
            if (room == null) return null;

            var months = room.Expenses
                .Select(e => e.Date.ToString("yyyy-MM"))
                .Distinct()
                .OrderByDescending(m => m)
                .ToList();

            return new RoomDetailsViewModel
            {
                Room = room,
                AvailableMonths = months,
                SelectedMonth = month ?? months.FirstOrDefault(),
                IsFromSettled = isFromSettled
            };
        }
    }
}
