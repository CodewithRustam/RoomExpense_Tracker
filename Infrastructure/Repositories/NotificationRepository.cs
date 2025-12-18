namespace Infrastructure.Repositories
{
    public class NotificationRepository: Repository<PushNotification>, INotificationRepository 
    {
        private readonly AppDbContext _context;
        public NotificationRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
