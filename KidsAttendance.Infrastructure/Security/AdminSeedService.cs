using KidsAttendance.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KidsAttendance.Infrastructure.Security;

public class AdminSeedService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IOptions<AdminSeedOptions> _options;
    private readonly ILogger<AdminSeedService> _logger;

    public AdminSeedService(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IOptions<AdminSeedOptions> options,
        ILogger<AdminSeedService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _options = options;
        _logger = logger;
    }

    public async Task EnsureSeedAsync()
    {
        var options = _options.Value;
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.Account) || string.IsNullOrWhiteSpace(options.Password))
        {
            return;
        }

        foreach (var roleName in new[] { ApplicationRoles.Coordinador, ApplicationRoles.Teacher, ApplicationRoles.SnackTeam })
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new AppRole { Name = roleName, NormalizedName = roleName.ToUpperInvariant() });
            }
        }

        var account = options.Account.Trim();
        var normalizedAccount = account.ToUpperInvariant();
        var existingUser = await _userManager.FindByNameAsync(account);
        if (existingUser is not null)
        {
            return;
        }

        var internalEmail = BuildInternalEmail(account);
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            FullName = options.FullName.Trim(),
            Email = internalEmail,
            NormalizedEmail = internalEmail.ToUpperInvariant(),
            UserName = account,
            NormalizedUserName = normalizedAccount,
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, options.Password);
        if (!createResult.Succeeded)
        {
            _logger.LogError("No se pudo crear el coordinador inicial: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
            return;
        }

        await _userManager.AddToRoleAsync(user, ApplicationRoles.Coordinador);
        _logger.LogInformation("Usuario coordinador inicial creado: {Account}", account);
    }

    private static string BuildInternalEmail(string account)
    {
        return $"{account}@local.invalid";
    }
}
