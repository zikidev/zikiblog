namespace ZikiBlog.Models;

public class User
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public bool IsApproved { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; }
}
