using System.Globalization;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace Services.ViewModels
{
    public class RoomDetailsViewModel
    {
        public Room Room { get; set; }
        public List<string> AvailableMonths { get; set; } = new();
        public string? SelectedMonth { get; set; }
        public bool IsFromSettled { get; set; }

        public bool HasExpenses => Room?.Expenses?.Any() ?? false;
        public IEnumerable<SelectListItem> Months =>
            AvailableMonths.Select(m => new SelectListItem
            {
                Value = m,
                Text = DateTime.ParseExact(m, "yyyy-MM", CultureInfo.InvariantCulture).ToString("MMMM yyyy"),
                Selected = m == SelectedMonth
            });
    }
}
