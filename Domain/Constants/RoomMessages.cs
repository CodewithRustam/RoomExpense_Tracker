namespace Domain.Constants
{
    public static class RoomMessages
    {
        public const string UserNotFound = "User not found.";
        public const string InvalidUser = "Invalid user.";
        public const string RoomCreated = "Room created successfully.";
        public const string RoomCreationFailed = "Failed to create room.";

        public static string UserDoesNotExist(string username) => $"User '{username}' does not exist.";
    }
}
