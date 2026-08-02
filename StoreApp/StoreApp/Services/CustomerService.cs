using StoreApp.Models.DTOs;
using StoreApp.Models.Entities;
using StoreApp.Repositories;

namespace StoreApp.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _repo;
        private readonly IOrderRepository _orders;

        public CustomerService(ICustomerRepository repo, IOrderRepository orders)
        {
            _repo = repo;
            _orders = orders;
        }

        public IEnumerable<CustomerDto> GetAll() => _repo.GetAll().Select(ToDto);

        public CustomerDto? GetById(int id)
        {
            var c = _repo.GetById(id);
            return c == null ? null : ToDto(c);
        }

        public IEnumerable<CustomerDto> Search(string? term) => _repo.Search(term).Select(ToDto);

        public CustomerDto Create(CustomerDto dto)
        {
            var entity = new Customer
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address,
                City = dto.City,
                Country = dto.Country
            };
            _repo.Add(entity);
            return ToDto(entity);
        }

        public void Update(CustomerDto dto)
        {
            var entity = _repo.GetById(dto.Id);
            if (entity == null) return;
            entity.FullName = dto.FullName;
            entity.Email = dto.Email;
            entity.Phone = dto.Phone;
            entity.Address = dto.Address;
            entity.City = dto.City;
            entity.Country = dto.Country;
            _repo.Update(entity);
        }

        public void Delete(int id)
        {
            var entity = _repo.GetById(id);
            if (entity != null) _repo.Delete(entity);
        }

        private CustomerDto ToDto(Customer c) => new()
        {
            Id = c.Id,
            FullName = c.FullName,
            Email = c.Email,
            Phone = c.Phone,
            Address = c.Address,
            City = c.City,
            Country = c.Country,
            OrdersCount = _orders.GetByCustomer(c.Id).Count()
        };
    }
}
