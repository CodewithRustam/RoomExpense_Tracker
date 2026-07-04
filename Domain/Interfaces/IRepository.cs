using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Domain.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null);

        Task<T?> GetByIdAsync(int id);

        Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

        Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

        Task AddAsync(T entity);

        Task AddRangeAsync(IEnumerable<T> entities);

        void Update(T entity); 

        void Delete(T entity);

        IQueryable<T> GetQueryable(); 
    }
}