using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using WeatherManagement.Infrastructure.Data;

namespace WeatherManagement.Infrastructure.Repo
{
    public interface IGenericRepository<TEntity, TKey> where TEntity : class
    {
        Task<TEntity?> FindByIdAsync(TKey id);
        Task<TEntity?> FindFirstAsync(Expression<Func<TEntity, bool>> predicate);
        Task<IEnumerable<TEntity>> ListAllAsync();
        Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
        Task InsertAsync(TEntity entity);
        Task InsertRangeAsync(IEnumerable<TEntity> entities);
    }

    public class GenericRepository<TEntity, TKey> : IGenericRepository<TEntity, TKey>
     where TEntity : class
    {
        private readonly WeatherDbContext _context;
        private readonly DbSet<TEntity> _dbSet;

        public GenericRepository(WeatherDbContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        public async Task<TEntity?> FindByIdAsync(TKey id) =>
            await _dbSet.FindAsync(id);

        public async Task<TEntity?> FindFirstAsync(Expression<Func<TEntity, bool>> predicate) =>
            await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate);

        public async Task<IEnumerable<TEntity>> ListAllAsync() =>
            await _dbSet.AsNoTracking().ToListAsync();

        public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate) =>
            await _dbSet.AsNoTracking().Where(predicate).ToListAsync();

        public async Task InsertAsync(TEntity entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task InsertRangeAsync(IEnumerable<TEntity> entities)
        {
            await _dbSet.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }
    }
}
