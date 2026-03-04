using ZikiBlog.Models;

namespace ZikiBlog.Data;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username);
    Task<int> CountAsync();
    Task CreateAsync(User user);
}
