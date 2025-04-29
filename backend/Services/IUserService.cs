using System.Collections.Generic;
using System.Threading.Tasks;
using backend.DTO;

namespace backend.Services
{
    public interface IUserService
    {
        IEnumerable<string> ActiveTokens { get; }
        Task CreateUserAsync(RegisterDTO dto);
        Task<string> LoginAsync(LoginDTO dto);
        void Logout(string token);
        Task<List<UserDTO>> GetUsersAsync();
        Task UpdateUserAsync(string name, RegisterDTO dto);
        Task DeleteUserAsync(string name);
    }
}