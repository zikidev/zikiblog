using System.Data;
using Dapper;
using ZikiBlog.Models;

namespace ZikiBlog.Data;

public class PostgresUserRepository : IUserRepository
{
    private readonly IDbConnection _db;
    public PostgresUserRepository(IDbConnection db) => _db = db;

    public async Task<User?> GetByUsernameAsync(string username)
    {
        const string sql = "SELECT id, username, password_hash AS PasswordHash, role, created_at AS CreatedAt FROM users WHERE username = @u LIMIT 1";
        return await _db.QueryFirstOrDefaultAsync<User>(sql, new { u = username });
    }

    public async Task<int> CountAsync()
        => await _db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users");

    public async Task CreateAsync(User user)
    {
        const string sql = "INSERT INTO users(id, username, password_hash, role, created_at) VALUES (@Id, @Username, @PasswordHash, @Role, now())";
        if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();
        await _db.ExecuteAsync(sql, user);
    }
}
