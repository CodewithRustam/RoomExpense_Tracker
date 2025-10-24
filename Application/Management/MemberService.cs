namespace Services.Management
{
    public class MemberService : IMemberServices
    {
        private readonly IMemberRepository memberRepository;
        private readonly ICurrentUserService currentUser;

        public MemberService(IMemberRepository _memberRepository, ICurrentUserService _currentUser) 
        {
            memberRepository = _memberRepository;
            currentUser = _currentUser;
        }
        public async Task<int> GetMemberId(int roomId)
        {
            string? userId = currentUser.UserId;
            return await memberRepository.GetMemberId(userId, roomId);
        }
    }
}
