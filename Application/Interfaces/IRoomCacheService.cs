namespace Services.Interfaces
{
    public interface IRoomCacheService
    {
        bool TryGetRooms(string userId, out IEnumerable<RoomResponse>? rooms);

        void SetRooms(string userId, IEnumerable<RoomResponse> rooms);

        void ClearRoomsCache(string userId);
    }
}
