namespace Services.Interfaces
{
    public interface IMemberServices
    {
        Task<int> GetMemberIdAsync(int roomId);
    }
}
