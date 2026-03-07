using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ZikiBlog.Services;

namespace ZikiBlog.Pages.Auth;

public class RegisterModel : PageModel
{
    private readonly UserService _userService;

    public RegisterModel(UserService userService)
    {
        _userService = userService;
    }

    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string username, string password, string confirmPassword)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Brukernavn og passord er påkrevd";
            return Page();
        }

        if (password != confirmPassword)
        {
            ErrorMessage = "Passordene stemmer ikke overens";
            return Page();
        }

        if (password.Length < 8)
        {
            ErrorMessage = "Passordet må være minst 8 tegn";
            return Page();
        }

        var existingUser = await _userService.GetByUsernameAsync(username);
        if (existingUser != null)
        {
            ErrorMessage = "Brukernavnet er allerede i bruk";
            return Page();
        }

        var created = await _userService.CreateUserAsync(username, password);
        if (created)
        {
            Success = true;
        }
        else
        {
            ErrorMessage = "En feil oppstod ved registrering";
        }

        return Page();
    }
}