using Google.Apis.PeopleService.v1;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebSite.Authentication;
using WebSite.Filters;
using WebSite.Models.Contexts;
using WebSite.Models.Entities;
using WebSite.Services.MapperProfiles;
using InvalidOperationException = System.InvalidOperationException;

namespace WebSite.Utils.Extensions;

public static class StartupExtensions
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddDbContext<SqlServerDatabaseContext>(options =>
        {
            options.UseSqlServer(builder.Configuration.GetConnectionString("Database"));
        });

        builder.Services.AddIdentity<User, IdentityRole>(
                options =>
                {
                    // Password settings
                    if (!builder.Environment.IsProduction())
                    {
                        options.Password.RequireDigit = true;
                        options.Password.RequiredLength = 3;
                        options.Password.RequireNonAlphanumeric = false;
                        options.Password.RequireUppercase = false;
                        options.Password.RequireLowercase = false;
                    }
                    else
                    {
                        options.Password.RequireDigit = true;
                        options.Password.RequiredLength = 8;
                        options.Password.RequireNonAlphanumeric = true;
                        options.Password.RequireUppercase = true;
                        options.Password.RequireLowercase = true;
                    }

                    // Lockout settings
                    if (builder.Environment.IsProduction())
                    {
                        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(value: 1);
                    }
                    else
                    {
                        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(value: 365);
                    }

                    options.Lockout.MaxFailedAccessAttempts = 5;
                    options.Lockout.AllowedForNewUsers = true;
                })
            .AddEntityFrameworkStores<SqlServerDatabaseContext>()
            .AddDefaultTokenProviders();

        builder.Services.AddAuthentication()
            .AddCookie(
                options =>
                {
                    options.Cookie.HttpOnly = true;
                    options.ExpireTimeSpan = TimeSpan.FromHours(value: 1);

                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                })
            .AddGoogle(options =>
            {
                const string ClientIdPath = "Authentication:Google:ClientId";
                options.ClientId = builder.Configuration[ClientIdPath] ?? throw new InvalidOperationException($"'{ClientIdPath}' configuration value is not provided.");
                const string ClientSecretPath = "Authentication:Google:ClientSecret";
                options.ClientSecret = builder.Configuration[ClientSecretPath] ??
                                       throw new InvalidOperationException($"'{ClientSecretPath}' configuration value is not provided.");

                options.SaveTokens = true;
                options.Scope.Add(PeopleServiceService.ScopeConstants.UserinfoEmail);
                options.Scope.Add(PeopleServiceService.ScopeConstants.UserinfoProfile);
                options.Events = new GoogleOAuthEvents();
            })
            .AddGitHub(options =>
            {
                const string ClientIdPath = "Authentication:GitHub:ClientId";
                options.ClientId = builder.Configuration[ClientIdPath] ?? throw new InvalidOperationException($"'{ClientIdPath}' configuration value is not provided.");
                const string ClientSecretPath = "Authentication:GitHub:ClientSecret";
                options.ClientSecret = builder.Configuration[ClientSecretPath] ??
                                       throw new InvalidOperationException($"'{ClientSecretPath}' configuration value is not provided.");
                
                options.Scope.Add("user:email");
            });

        builder.Services.AddAuthorization(
            options =>
            {
                options.AddPolicy(name: "Authenticated",
                    policy => policy.RequireAuthenticatedUser());
            });

        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(
            options =>
            {
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.IdleTimeout = TimeSpan.FromMinutes(value: 60);
            });

        builder.Services.AddAutoMapper(
            profiles => { profiles.AddProfile<UserRegistrationProfile>(); });

        builder.Services.AddHttpClient("AzureFunctionClient", httpClient =>
        {
            const string AzureFunctionBaseUrlPath = "Azure:FunctionApp:BaseAddress";
            var azureFunctionBaseUrl = builder.Configuration[AzureFunctionBaseUrlPath] ?? throw new InvalidOperationException($"'{AzureFunctionBaseUrlPath}' configuration value is not provided");
            
            httpClient.BaseAddress = new Uri(azureFunctionBaseUrl);
        });

        builder.Services.AddControllersWithViews(options =>
        {
            options.Filters.Add<KeepModelErrorsOnRedirectAttribute>();
            options.Filters.Add<RetrieveModelErrorsFromRedirectorAttribute>();
        });
        
        return builder;
    }

    public static void Configure(this WebApplication app)
    {
        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseSession();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "home_page",
            pattern: "Home/{page}",
            defaults: new { controller = "Image", action = "Home" });
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Image}/{action=Home}/{id?}");
    }
}