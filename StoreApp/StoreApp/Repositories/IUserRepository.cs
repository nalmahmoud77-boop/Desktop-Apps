using StoreApp.Models.Entities;

namespace StoreApp.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        User? GetByUsername(string username);
    }
}
