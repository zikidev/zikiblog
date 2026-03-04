# Ziki Blog (Secure login with antiforgery, Bootstrap, Blazor Server, PostgreSQL, Dapper)

This build implements **Option B**: cookie sign-in via Minimal API with **antiforgery middleware enabled**, and a **Razor Page login** that posts a token to `/auth/signin`. UI uses **Bootstrap 5**.

## Run
1) Update `appsettings.Development.json` with your PostgreSQL connection string.
2) Ensure your DB user owns the DB/schema or has create rights.
3) `dotnet restore` and `dotnet run`.

Seed admin (first run): `admin` / `ChangeMe123!` (change immediately).

## Key files
- `Program.cs`: `app.UseAntiforgery()` + endpoints `/auth/signin`, `/auth/signout`.
- `Pages/Auth/Login.cshtml`: Razor Page login with `@Html.AntiForgeryToken()`.
- Blazor pages under `Pages/*.razor` and shared layout under `Shared/`.
