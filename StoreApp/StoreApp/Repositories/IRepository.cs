using System.Linq.Expressions;

namespace StoreApp.Repositories
{
    public interface IRepository<T> where T : class
    {
        IEnumerable<T> GetAll();
        T? GetById(int id);
        IEnumerable<T> Find(Expression<Func<T, bool>> predicate);
        T Add(T entity);
        void Update(T entity);
        void Delete(T entity);
        int Count();
    }
}
