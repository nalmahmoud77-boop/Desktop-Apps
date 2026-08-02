using StoreApp.Models.DTOs;

namespace StoreApp.Services
{
    public interface IAuthService
    {
        UserDto? Login(LoginDto dto);
        void Logout();
    }
}
