using System.ComponentModel.DataAnnotations;

namespace WebSite.ViewModels.Account;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter an email address.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Please enter a password.")]
    [MinLength(8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;
}