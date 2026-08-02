using StoreApp.Models.DTOs;

namespace StoreApp.Services
{
    public interface ICustomerService
    {
        IEnumerable<CustomerDto> GetAll();
        CustomerDto? GetById(int id);
        IEnumerable<CustomerDto> Search(string? term);
        CustomerDto Create(CustomerDto dto);
        void Update(CustomerDto dto);
        void Delete(int id);
    }
}
