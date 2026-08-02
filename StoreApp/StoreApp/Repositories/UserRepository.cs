using StoreApp.Data;
using StoreApp.Models.Entities;

namespace StoreApp.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(StoreDbContext context) : base(context) { }

        public User? GetByUsername(string username) =>
            _set.FirstOrDefault(u => u.Username.ToLower() == username.ToLower());
    }
}
