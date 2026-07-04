namespace Services.Interfaces
{
    public interface IRoomMapper
    {
        IReadOnlyList<RoomResponse> MapToRoomResponses(IReadOnlyList<Room> rooms);
        RoomDetailsViewModel? MapToRoomDetailsViewModel(Room? room, string? month, bool isFromSettled);
    }
}
