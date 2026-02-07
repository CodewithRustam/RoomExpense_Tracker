using Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System.Collections;
using System.Reflection;

namespace Infrastructure
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;
        private Hashtable? _repositories;
        private readonly IServiceProvider _serviceProvider; 
        public UnitOfWork(AppDbContext context, IConfiguration config, IMemoryCache cache, IServiceProvider serviceProvider)
        {
            _context = context;
            _config = config;
            _cache = cache;
            _serviceProvider = serviceProvider; 
        }

        public IRepository<TEntity> Repository<TEntity>() where TEntity : class
        {
            _repositories ??= new Hashtable();
            var typeName = typeof(TEntity).Name;

            if (!_repositories.ContainsKey(typeName))
            {
                // Find the custom repo (e.g., ExpensesRepository)
                var customRepoType = Assembly.GetExecutingAssembly()
                    .GetTypes()
                    .FirstOrDefault(t => typeof(IRepository<TEntity>).IsAssignableFrom(t)
                                         && !t.IsInterface
                                         && !t.IsAbstract);

                object repositoryInstance;

                if (customRepoType != null)
                {
                    // ActivatorUtilities uses _serviceProvider to find IMemoryCache and IConfiguration
                    // and manually passes _context as the third required piece.
                    repositoryInstance = ActivatorUtilities.CreateInstance(_serviceProvider, customRepoType, _context);
                }
                else
                {
                    var genericRepoType = typeof(Repository<>).MakeGenericType(typeof(TEntity));
                    repositoryInstance = ActivatorUtilities.CreateInstance(_serviceProvider, genericRepoType, _context);
                }

                _repositories.Add(typeName, repositoryInstance);
            }

            return (IRepository<TEntity>)_repositories[typeName]!;
        }

        public async Task<int> SaveAsync() => await _context.SaveChangesAsync();

        public void Dispose() => _context.Dispose();
    }
}
