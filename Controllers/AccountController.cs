using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebSite.Filters;
using WebSite.Models.Entities;
using WebSite.Utils.Extensions;
using WebSite.ViewModels.Account;

namespace WebSite.Controllers;

public class AccountController : Controller
{
private readonly UserManager<User> userManager;
    private readonly SignInManager<User> signManager;
    private readonly IMapper mapper;
    private readonly ILogger<AccountController> logger;

    public AccountController(UserManager<User> userManager, SignInManager<User> signManager, IMapper mapper, ILoggerFactory loggerFactory)
    {
        this.userManager = userManager;
        this.signManager = signManager;
        this.mapper = mapper;
        logger = loggerFactory.CreateLogger<AccountController>();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        logger.LogInformation("[GET] Register: returning view");
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegistrationViewModel registrationViewModel, string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            logger.LogWarning("[POST] Register: model contains errors, returning view");
            return View(registrationViewModel);
        }

        var user = mapper.Map<User>(registrationViewModel);
        user.LockoutEnabled = false;
        var password = registrationViewModel.Password;
        var registrationResult = await userManager.CreateAsync(user, password);

        if (registrationResult.Succeeded)
        {
            await signManager.SignInAsync(user, isPersistent: false);
            logger.LogInformation("[POST] Register: successfully registered user {Email}", user.Email);
            return !string.IsNullOrWhiteSpace(returnUrl) ? RedirectToLocal(returnUrl) : RedirectToHome();
        }

        TryAddModelErrorsFromResult(registrationResult);
        logger.LogWarning("[POST] Register: registration result is not succeeded, returning view");
        return View(registrationViewModel);
    }

    [HttpGet]
    [AllowAnonymous]
    [RetrieveModelErrorsFromRedirector]
    public IActionResult Login()
    {
        logger.LogInformation("[GET] Login: returning view");
        return View(new LoginViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel loginViewModel)
    {
        if (string.IsNullOrWhiteSpace(loginViewModel.Email))
        {
            ModelState.AddSummaryErrorForProperty(nameof(LoginViewModel.Email),
                errorMessage: "The email field is required.");
        }

        if (string.IsNullOrWhiteSpace(loginViewModel.Password))
        {
            ModelState.AddSummaryErrorForProperty(nameof(LoginViewModel.Password),
                errorMessage: "The password field is required.");
        }

        if (!ModelState.IsValid)
        {
            logger.LogWarning("[POST] Login: model contains errors, returning view");
            return View(loginViewModel);
        }

        var user = await userManager.FindByEmailAsync(loginViewModel.Email);
        if (user is null)
        {
            ModelState.AddSummaryErrorForProperty(nameof(LoginViewModel.Email),
                errorMessage: "No user found with this email.");
            
            logger.LogWarning("[POST] Login: model contains errors, returning view");
            return View(loginViewModel);
        }

        var result = await signManager.PasswordSignInAsync(
            user, loginViewModel.Password, isPersistent: true, lockoutOnFailure: true);
        if (result.Succeeded)
        {
            logger.LogInformation("[POST] Login: successfully executed Login for user {Email}, redirecting to Image.Home", user.Email);
            return RedirectToHome();
        }

        string errorMessage;
        if (await userManager.IsLockedOutAsync(user))
        {
            errorMessage =
                "Your account has been blocked due to a high number of failed login attempts.";
        }
        else
        {
            const int MaxFailedAccessAttempts = 5;
            int remainingAccessTries =
                MaxFailedAccessAttempts - await userManager.GetAccessFailedCountAsync(user);

            errorMessage = remainingAccessTries > 3
                ? "Wrong password. Please try again."
                : $"Wrong password. Remaining tries: {remainingAccessTries}";
        }

        ModelState.AddSummaryErrorForProperty(nameof(LoginViewModel.Password), errorMessage);
        logger.LogWarning("[POST] Login: user {Email} login result is not succeeded, returning view", user.Email);
        return View(loginViewModel);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ExternalLogin(string provider, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(provider))
            throw new ArgumentNullException(nameof(provider));

        logger.LogInformation("[GET] ExternalLogin: executing external login request (provider: {Provider})", provider);
        
        var redirectUrl = Url.Action(action: nameof(ExternalLoginCallback), controller: "Account",
            values: new { returnUrl });

        var properties = signManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return Challenge(properties, provider);
    }

    [HttpGet]
    [AllowAnonymous]
    [KeepModelErrorsOnRedirect]
    public async Task<IActionResult> ExternalLoginCallback(string returnUrl = "/")
    {
        var info = await signManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            logger.LogWarning("[GET] ExternalLoginCallback: external login failed for a third-party reason");
            return RedirectToLoginForcibly(returnUrl);
        }

        var email = (info.Principal.FindFirstValue(ClaimTypes.Email) ?? info.Principal.FindFirstValue(ClaimTypes.NameIdentifier))!;
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
            var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname);

            user = new User { Email = email, UserName = email, FirstName = firstName, LastName = lastName, LockoutEnabled = false };

            await userManager.CreateAsync(user);
            logger.LogInformation("[GET] ExternalLoginCallback: successfully registered new user {Email} through external login", user.Email);
        }

        var addingLoginResult = await userManager.AddLoginAsync(user, info);
        if (addingLoginResult.Succeeded)
        {
            logger.LogInformation("[GET] ExternalLoginCallback: successfully added new login to user {Email} through external login", user.Email);
        }

        var result = await signManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey,
            isPersistent: false, bypassTwoFactor: true);

        if (result.Succeeded)
        {
            logger.LogInformation("[GET] ExternalLoginCallback: successfully logged in user {Email} through {LoginProvider} login", user.Email, info.LoginProvider);
            return RedirectToLocal(returnUrl);
        }
        
        logger.LogWarning("[GET] ExternalLoginCallback: user {Email} external login result through {LoginProvider} is not succeeded, redirecting to login", user.Email, info.LoginProvider);
        return RedirectToLoginForcibly(returnUrl);
    }

    [HttpGet]
    [Authorize(policy: "Authenticated")]
    public async Task<IActionResult> Logout()
    {
        logger.LogInformation("[GET] Logout: signing out user {Email}", User.Identity?.Name);
        await signManager.SignOutAsync();
        return RedirectToHome();
    }

    #region Utils

    private IActionResult RedirectToHome()
    {
        return RedirectToAction(controllerName: "Image", actionName: nameof(ImageController.Home));
    }

    private IActionResult RedirectToLocal(string? url)
    {
        if (!string.IsNullOrWhiteSpace(url) && Url.IsLocalUrl(url))
        {
            return Redirect(url);
        }
        else
        {
            return RedirectToHome();
        }
    }

    private IActionResult RedirectToLoginForcibly(string returnUrl)
    {
        ModelState.AddSummaryError("Something went wrong.");
        return RedirectToAction(actionName: nameof(Login), routeValues: new { returnUrl });
    }

    private void TryAddModelErrorsFromResult(IdentityResult? result)
    {
        if (result?.Errors.Any() is true)
        {
            foreach (var error in result.Errors)
                ModelState.AddSummaryError(error.Description);
        }
    }
    
    #endregion
}