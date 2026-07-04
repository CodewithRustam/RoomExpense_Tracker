namespace Infrastructure.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        public async Task<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null)
        {
            IQueryable<T> query = _dbSet.AsNoTracking();

            if (predicate != null)
                query = query.Where(predicate);

            return await query.ToListAsync();
        }

        public async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);

        public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate);

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
            => await _dbSet.AnyAsync(predicate);

        public Task AddAsync(T entity) => _dbSet.AddAsync(entity).AsTask();

        public Task AddRangeAsync(IEnumerable<T> entities) => _dbSet.AddRangeAsync(entities);

        public void Update(T entity) => _dbSet.Update(entity);

        public void Delete(T entity) => _dbSet.Remove(entity);

        public IQueryable<T> GetQueryable() => _dbSet.AsNoTracking();
    }
}