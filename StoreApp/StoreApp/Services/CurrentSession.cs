using StoreApp.Models.DTOs;

namespace StoreApp.Services
{
    public class CurrentSession
    {
        public UserDto? User { get; set; }
        public bool IsAuthenticated => User != null;
    }
}
