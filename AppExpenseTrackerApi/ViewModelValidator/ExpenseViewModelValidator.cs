using FluentValidation;
using System.Text.RegularExpressions;

namespace AppExpenseTrackerApi.ViewModelValidator
{
    public class ExpenseViewModelValidator : AbstractValidator<ExpenseViewModel>
    {
        public ExpenseViewModelValidator()
        {
            RuleFor(x => x.Item)
               .NotEmpty()
               // 1. Stricter Repeating: Change {4,} to {3,} to catch "aaaa"
               .Must(x => !Regex.IsMatch(x, @"(.)\1{3,}"))
               .WithMessage("Item name contains too many repeating characters.")
               
               // 2. Consecutive Consonants: Catch "chdbdnsndnn"
               // This looks for 6 consonants in a row (very rare in real words)
               .Must(x => !Regex.IsMatch(x, @"(?i)[bcdfghjklmnpqrstvwxyz]{6,}"))
               .WithMessage("Item name appears to be gibberish.")
               
               // 3. Length: Just in case it's too long
               .MaximumLength(30).WithMessage("Item name is too long.");

            RuleFor(x => x.Amount)
                .GreaterThan(1).WithMessage("Please enter a valid amount greater than ₹1.");

            RuleFor(x => x.RoomId)
                .GreaterThan(0).WithMessage("Invalid room selection.");

            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("Date is required.")
                .Must(BeAValidDate).WithMessage("Expense date cannot be in the future.");
        }
        private bool BeAValidDate(DateTime date)
        {
            return date <= DateTime.Now;
        }
        private bool NotHaveExcessiveRepeatingChars(string item)
        {
            if (string.IsNullOrEmpty(item)) return true;
            // Matches 5 or more identical characters in a row
            return !Regex.IsMatch(item, @"(.)\1{4,}");
        }

        private bool HaveVowelsIfLong(string item)
        {
            if (string.IsNullOrEmpty(item) || item.Length <= 5) return true;
            // Checks if there is at least one vowel
            return Regex.IsMatch(item, @"[aeiouAEIOU]");
        }

        private bool NotBeKeyboardPattern(string item)
        {
            if (string.IsNullOrEmpty(item)) return true;
            string lower = item.ToLower();
            string[] patterns = { "qwerty", "asdfgh", "zxcvbn", "123456" };
            return !patterns.Any(p => lower.Contains(p));
        }
    }
}
