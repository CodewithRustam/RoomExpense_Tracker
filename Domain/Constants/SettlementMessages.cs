namespace Domain.Constants
{
    public static class SettlementMessages
    {
        public const string InvalidRoomId = "Invalid room ID.";
        public const string InvalidPayer = "Invalid payer name.";
        public const string InvalidReceiver = "Invalid receiver name.";
        public const string InvalidAmount = "Settlement amount must be greater than zero.";
        public const string RoomNotFound = "Room not found.";
        public const string PayerNotFound = "Payer not found.";
        public const string ReceiverNotFound = "Receiver not found.";
        public const string SelfSettlement = "You cannot settle with yourself.";
        public const string NoMembers = "No members found in this room.";
        public const string PayerDoesNotOwe = "You do not owe any amount.";
        public const string ReceiverNotOwed = "Receiver is not owed any amount.";
        public const string UnexpectedError = "An unexpected error occurred while settling expenses. Please try again.";

        public static string ExceedsMax(decimal max) => $"Settlement cannot exceed ₹{max:F2}";
        public static string Success(decimal amount, string receiver) => $"Successfully settled ₹{amount:F2} with {receiver}.";
        public static string Failed(decimal amount, string receiver) => $"Settlement failed ₹{amount:F2} with {receiver}.";
    }
}
