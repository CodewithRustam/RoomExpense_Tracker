namespace Services.Management
{
    public class MemberService : IMemberServices
    {
        private readonly IMemberRepository _memberRepository;
        private readonly ICurrentUserService _currentUser;

        public MemberService(IMemberRepository memberRepository, ICurrentUserService currentUser)
        {
            _memberRepository = memberRepository ?? throw new ArgumentNullException(nameof(memberRepository));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        }

        public async Task<int> GetMemberIdAsync(int roomId)
        {
            string? userId = _currentUser.UserId;
            if (string.IsNullOrEmpty(userId))
            {
                return 0;
            }
            return await _memberRepository.GetMemberIdAsync(userId, roomId);
        }
    }
}