using StoreApp.Data;
using StoreApp.Models.Entities;

namespace StoreApp.Repositories
{
    public class CustomerRepository : Repository<Customer>, ICustomerRepository
    {
        public CustomerRepository(StoreDbContext context) : base(context) { }

        public IEnumerable<Customer> Search(string? term)
        {
            if (string.IsNullOrWhiteSpace(term)) return GetAll();
            var t = term.Trim().ToLowerInvariant();
            return _set.AsEnumerable().Where(c =>
                c.FullName.ToLowerInvariant().Contains(t) ||
                c.Email.ToLowerInvariant().Contains(t) ||
                c.Phone.ToLowerInvariant().Contains(t)).ToList();
        }
    }
}
