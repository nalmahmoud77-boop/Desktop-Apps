using StoreApp.Models.Entities;

namespace StoreApp.Repositories
{
    public interface ICustomerRepository : IRepository<Customer>
    {
        IEnumerable<Customer> Search(string? term);
    }
}
