namespace Services.Interfaces
{
    public interface IMemberServices
    {
        Task<int> GetMemberId(int roomId);
    }
}
