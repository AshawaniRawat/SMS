using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SchoolERP.Infrastructure.Identity;

namespace SchoolERP.Infrastructure.Persistence.Seed;

public sealed class DevelopmentAdminSeeder(IOptions<DevelopmentBootstrapOptions> options,
    UserManager<ApplicationUser> userManager, IHostEnvironment environment)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var bootstrap = options.Value;
        if (!environment.IsDevelopment() || !bootstrap.Enabled) return;
        if (string.IsNullOrWhiteSpace(bootstrap.AdminEmail) || string.IsNullOrWhiteSpace(bootstrap.AdminPassword))
            throw new InvalidOperationException("Development bootstrap admin email and password are required when enabled.");
        var email = bootstrap.AdminEmail.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = ApplicationUser.Create("Platform", "Administrator", email, activate: true, emailConfirmed: true);
            var created = await userManager.CreateAsync(user, bootstrap.AdminPassword);
            if (!created.Succeeded)
                throw new InvalidOperationException("Development platform administrator could not be created: " +
                    string.Join(", ", created.Errors.Select(x => x.Code)));
        }
        if (!await userManager.IsInRoleAsync(user, SystemRoleCodes.PlatformSuperAdmin))
        {
            var assigned = await userManager.AddToRoleAsync(user, SystemRoleCodes.PlatformSuperAdmin);
            if (!assigned.Succeeded)
                throw new InvalidOperationException("Development platform administrator role could not be assigned: " +
                    string.Join(", ", assigned.Errors.Select(x => x.Code)));
        }
    }
}
