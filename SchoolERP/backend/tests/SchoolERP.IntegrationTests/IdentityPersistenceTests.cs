using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SchoolERP.Application.Features.Identity;
using SchoolERP.Domain.Entities;
using SchoolERP.Infrastructure.Identity;
using SchoolERP.Infrastructure.Persistence.Context;
using Xunit;

namespace SchoolERP.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class IdentityPersistenceTests(PostgresFixture fixture)
{
    [PostgresFact]
    public async Task Identity_creation_hashes_password_supports_case_insensitive_lookup_and_rejects_duplicate()
    {
        await using var context = fixture.CreateContext(platformAccess: true);
        using var manager = CreateManager(context);
        var user = ApplicationUser.Create("Grace", "Hopper", "grace@example.com", activate: true);
        var created = await manager.CreateAsync(user, "Valid#Pass1");
        Assert.True(created.Succeeded, string.Join("; ", created.Errors.Select(x => x.Description)));
        Assert.NotNull(user.PasswordHash); Assert.NotEqual("Valid#Pass1", user.PasswordHash);
        Assert.True(await manager.CheckPasswordAsync(user, "Valid#Pass1"));
        Assert.False(await manager.CheckPasswordAsync(user, "wrong"));
        Assert.Equal(user.Id, (await manager.FindByEmailAsync("GRACE@EXAMPLE.COM"))!.Id);
        var duplicate = await manager.CreateAsync(ApplicationUser.Create("Other", "Person", "GRACE@example.com"), "Valid#Pass1");
        Assert.False(duplicate.Succeeded);
    }

    [PostgresFact]
    public async Task Password_policy_rejects_each_missing_requirement()
    {
        await using var context = fixture.CreateContext(platformAccess: true);
        using var manager = CreateManager(context);
        foreach (var password in new[] { "Aa#1", "lower#123", "UPPER#123", "NoDigits#A", "NoSpecial1A" })
        {
            var user = ApplicationUser.Create("Policy", "Test", $"{Guid.NewGuid():N}@example.com");
            Assert.False((await manager.CreateAsync(user, password)).Succeeded);
        }
        Assert.True((await manager.CreateAsync(ApplicationUser.Create("Policy", "Valid", $"{Guid.NewGuid():N}@example.com"), "Valid#Pass1")).Succeeded);
    }

    [PostgresFact]
    public async Task Migration_uses_clean_identity_table_names_and_preserves_domain_tables()
    {
        await using var context = fixture.CreateContext(platformAccess: true);
        var tables = await context.Database.SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = 'public'").ToListAsync();
        foreach (var expected in new[] { "users", "roles", "user_roles", "user_claims", "user_logins", "user_tokens", "role_claims", "tenants", "schools", "campuses", "academic_years" })
            Assert.Contains(expected, tables);
        Assert.DoesNotContain("AspNetUsers", tables); Assert.DoesNotContain("AspNetRoles", tables);
    }

    [PostgresFact]
    public void Organization_foreign_keys_are_restrict()
    {
        using var context = fixture.CreateContext(platformAccess: true);
        var user = context.Model.FindEntityType(typeof(ApplicationUser))!;
        var organizationFks = user.GetForeignKeys().Where(x => x.PrincipalEntityType.ClrType.Namespace == "SchoolERP.Domain.Entities");
        Assert.All(organizationFks, fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
    }

    [PostgresFact]
    public async Task Creation_service_enforces_tenant_school_campus_hierarchy_and_allows_platform_users()
    {
        await using var context = fixture.CreateContext(platformAccess: true);
        var tenantA = Tenant.Create("Tenant A", $"tenant-a-{Guid.NewGuid():N}");
        var tenantB = Tenant.Create("Tenant B", $"tenant-b-{Guid.NewGuid():N}");
        var schoolA = School.Create(tenantA, "School A", "A"); var schoolB = School.Create(tenantB, "School B", "B");
        var campusA = Campus.Create(schoolA, "Campus A", "A");
        context.AddRange(tenantA, tenantB, schoolA, schoolB, campusA); await context.SaveChangesAsync();
        using var manager = CreateManager(context);
        var service = new UserIdentityService(manager, context, new CreateUserRequestValidator(), TimeProvider.System,
            NullLogger<UserIdentityService>.Instance);
        var valid = await service.CreateUserAsync(new("Valid", "Tenant", $"{Guid.NewGuid():N}@example.com", "Valid#Pass1", tenantA.Id, schoolA.Id, campusA.Id));
        Assert.True(valid.IsSuccess, valid.Error);
        var crossTenant = await service.CreateUserAsync(new("Wrong", "School", $"{Guid.NewGuid():N}@example.com", "Valid#Pass1", tenantA.Id, schoolB.Id));
        Assert.False(crossTenant.IsSuccess);
        var wrongSchool = await service.CreateUserAsync(new("Wrong", "Campus", $"{Guid.NewGuid():N}@example.com", "Valid#Pass1", tenantA.Id, schoolB.Id, campusA.Id));
        Assert.False(wrongSchool.IsSuccess);
        var platform = await service.CreateUserAsync(new("Platform", "User", $"{Guid.NewGuid():N}@example.com", "Valid#Pass1"));
        Assert.True(platform.IsSuccess, platform.Error);
    }

    private static UserManager<ApplicationUser> CreateManager(SchoolErpDbContext context)
    {
        var options = Options.Create(new IdentityOptions());
        options.Value.User.RequireUniqueEmail = true;
        options.Value.Password.RequiredLength = 8; options.Value.Password.RequiredUniqueChars = 4;
        options.Value.Password.RequireDigit = true; options.Value.Password.RequireLowercase = true;
        options.Value.Password.RequireUppercase = true; options.Value.Password.RequireNonAlphanumeric = true;
        var store = new UserStore<ApplicationUser, ApplicationRole, SchoolErpDbContext, Guid>(context);
        return new UserManager<ApplicationUser>(store, options, new PasswordHasher<ApplicationUser>(),
            [new UserValidator<ApplicationUser>()], [new PasswordValidator<ApplicationUser>()],
            new UpperInvariantLookupNormalizer(), new IdentityErrorDescriber(), null!, NullLogger<UserManager<ApplicationUser>>.Instance);
    }
}
