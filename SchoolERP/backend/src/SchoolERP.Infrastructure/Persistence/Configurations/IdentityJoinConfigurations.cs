using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SchoolERP.Infrastructure.Persistence.Configurations;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>> { public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> b) { b.ToTable("user_roles"); b.Property(x => x.UserId).HasColumnName("user_id"); b.Property(x => x.RoleId).HasColumnName("role_id"); } }
public sealed class UserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>> { public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> b) { b.ToTable("user_claims"); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.UserId).HasColumnName("user_id"); b.Property(x => x.ClaimType).HasColumnName("claim_type"); b.Property(x => x.ClaimValue).HasColumnName("claim_value"); } }
public sealed class UserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>> { public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> b) { b.ToTable("user_logins"); b.Property(x => x.LoginProvider).HasColumnName("login_provider"); b.Property(x => x.ProviderKey).HasColumnName("provider_key"); b.Property(x => x.ProviderDisplayName).HasColumnName("provider_display_name"); b.Property(x => x.UserId).HasColumnName("user_id"); } }
public sealed class UserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>> { public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> b) { b.ToTable("user_tokens"); b.Property(x => x.UserId).HasColumnName("user_id"); b.Property(x => x.LoginProvider).HasColumnName("login_provider"); b.Property(x => x.Name).HasColumnName("name"); b.Property(x => x.Value).HasColumnName("value"); } }
public sealed class RoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>> { public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> b) { b.ToTable("role_claims"); b.Property(x => x.Id).HasColumnName("id"); b.Property(x => x.RoleId).HasColumnName("role_id"); b.Property(x => x.ClaimType).HasColumnName("claim_type"); b.Property(x => x.ClaimValue).HasColumnName("claim_value"); } }
