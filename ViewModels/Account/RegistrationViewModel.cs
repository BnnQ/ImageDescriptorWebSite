using System.ComponentModel.DataAnnotations;

namespace WebSite.ViewModels.Account;

public class RegistrationViewModel
{
    [Required(ErrorMessage = "Please enter a first name.")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = null!;
    
    [Required(ErrorMessage = "Please enter a last name.")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = null!;
    
    [Required(ErrorMessage = "Please enter an email address.")]
    [EmailAddress(ErrorMessage = "Invalid email address.")]
    public string Email { get; set; } = null!;
    
    [Required(ErrorMessage = "Please enter a username.")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Please enter a password.")]
    [MinLength(8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = null!;
}