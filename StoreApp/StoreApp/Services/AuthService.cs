using StoreApp.Models.DTOs;
using StoreApp.Repositories;

namespace StoreApp.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _users;
        private readonly CurrentSession _session;

        public AuthService(IUserRepository users, CurrentSession session)
        {
            _users = users;
            _session = session;
        }

        public UserDto? Login(LoginDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return null;

            var user = _users.GetByUsername(dto.Username);
            if (user == null || !user.IsActive) return null;
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash)) return null;

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            };

            _session.User = userDto;
            return userDto;
        }

        public void Logout() => _session.User = null;
    }
}
