using Dapper;
using Npgsql;
using ZikiBlog.Models;

namespace ZikiBlog.Services;

public class UserService
{
    private readonly string _connectionString;

    public UserService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<User?> GetByUsernameAsync(string username)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<User>(
            "SELECT id, username, password_hash AS PasswordHash, role, is_approved AS IsApproved, created_at AS CreatedAt FROM users WHERE username = @Username",
            new { Username = username });
    }

    public async Task<bool> CreateUserAsync(string username, string password)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        await using var conn = new NpgsqlConnection(_connectionString);
        
        var result = await conn.ExecuteAsync(
            "INSERT INTO users (username, password_hash, role, is_approved) VALUES (@Username, @PasswordHash, 'User', false)",
            new { Username = username, PasswordHash = passwordHash });
        
        return result > 0;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var users = await conn.QueryAsync<User>(
            "SELECT id, username, password_hash AS PasswordHash, role, is_approved AS IsApproved, created_at AS CreatedAt FROM users ORDER BY created_at DESC");
        return users.ToList();
    }

    public async Task<bool> ApproveUserAsync(Guid userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var result = await conn.ExecuteAsync(
            "UPDATE users SET is_approved = true WHERE id = @UserId",
            new { UserId = userId });
        return result > 0;
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        await using var conn = new NpgsqlConnection(_connectionString);
        var result = await conn.ExecuteAsync(
            "DELETE FROM users WHERE id = @UserId",
            new { UserId = userId });
        return result > 0;
    }

    public async Task<bool> ValidatePasswordAsync(string username, string password)
    {
        var user = await GetByUsernameAsync(username);
        if (user == null) return false;
        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }
}