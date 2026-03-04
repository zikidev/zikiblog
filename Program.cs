using System.Data;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using ZikiBlog.Data;
using ZikiBlog.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? builder.Configuration["ConnectionStrings:DefaultConnection"]
    ?? "Host=localhost;Port=5432;Database=blazor_blog;Username=postgres;Password=postgres";

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/signout";
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

builder.Services.AddSingleton(new DbConfig(connectionString));
builder.Services.AddScoped<IDbConnection>(sp => new NpgsqlConnection(connectionString));

builder.Services.AddScoped<IUserRepository, PostgresUserRepository>();
builder.Services.AddScoped<IPostRepository, PostgresPostRepository>();

builder.Services.AddScoped<SlugService>();
builder.Services.AddScoped<BloggerImportService>();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Ensure DB schema and seed admin
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();
    await DbInitializer.EnsureDatabaseAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Enable antiforgery middleware (required for endpoints that expect tokens)
app.UseAntiforgery();

// Minimal API endpoints for sign-in/out (with antiforgery token expected by default)
app.MapPost("/auth/signin", async (
    HttpContext http,
    IUserRepository users,
    [FromForm] LoginDto form) =>
{
    var user = await users.GetByUsernameAsync(form.Username);
    if (user is null || !BCrypt.Net.BCrypt.Verify(form.Password, user.PasswordHash))
    {
        return Results.Redirect("/auth/login?error=1");
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, user.Username),
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new(ClaimTypes.Role, user.Role)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
        new AuthenticationProperties { IsPersistent = true });

    return Results.Redirect("/");
});

app.MapGet("/auth/signout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
});

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

public record LoginDto(string Username, string Password);

namespace ZikiBlog.Data
{
    public static class DbInitializer
    {
        public static async Task EnsureDatabaseAsync(IDbConnection db)
        {
            const string usersSql = @"CREATE TABLE IF NOT EXISTS users (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                username TEXT NOT NULL UNIQUE,
                password_hash TEXT NOT NULL,
                role TEXT NOT NULL DEFAULT 'User',
                created_at TIMESTAMPTZ NOT NULL DEFAULT now()
            );";

            const string postsSql = @"CREATE TABLE IF NOT EXISTS posts (
                id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                title TEXT NOT NULL,
                slug TEXT NOT NULL UNIQUE,
                content_html TEXT NOT NULL,
                summary TEXT NULL,
                published_at TIMESTAMPTZ NULL,
                updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                is_published BOOLEAN NOT NULL DEFAULT true
            );
            CREATE INDEX IF NOT EXISTS idx_posts_published_at ON posts(published_at DESC);
            CREATE INDEX IF NOT EXISTS idx_posts_slug ON posts(slug);
            ";

            const string ext = "CREATE EXTENSION IF NOT EXISTS pgcrypto;";

            await db.ExecuteAsync(ext);
            await db.ExecuteAsync(usersSql);
            await db.ExecuteAsync(postsSql);

            var userCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM users;");
            if (userCount == 0)
            {
                var defaultUser = "admin";
                var defaultPass = "ChangeMe123!";
                var hash = BCrypt.Net.BCrypt.HashPassword(defaultPass);
                await db.ExecuteAsync("INSERT INTO users(username, password_hash, role) VALUES (@u, @p, 'Admin')",
                    new { u = defaultUser, p = hash });
                Console.WriteLine($"Seeded default admin user '{defaultUser}' with password '{defaultPass}'. PLEASE CHANGE IMMEDIATELY.");
            }
        }
    }

    public class DbConfig
    {
        public string ConnectionString { get; }
        public DbConfig(string cs) => ConnectionString = cs;
    }
}
