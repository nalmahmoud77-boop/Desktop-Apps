using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StoreApp.Data;

namespace StoreApp.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly StoreDbContext _context;
        protected readonly DbSet<T> _set;

        public Repository(StoreDbContext context)
        {
            _context = context;
            _set = context.Set<T>();
        }

        public virtual IEnumerable<T> GetAll() => _set.ToList();

        public virtual T? GetById(int id) => _set.Find(id);

        public virtual IEnumerable<T> Find(Expression<Func<T, bool>> predicate) => _set.Where(predicate).ToList();

        public virtual T Add(T entity)
        {
            _set.Add(entity);
            _context.SaveChanges();
            return entity;
        }

        public virtual void Update(T entity)
        {
            _set.Update(entity);
            _context.SaveChanges();
        }

        public virtual void Delete(T entity)
        {
            _set.Remove(entity);
            _context.SaveChanges();
        }

        public virtual int Count() => _set.Count();
    }
}
