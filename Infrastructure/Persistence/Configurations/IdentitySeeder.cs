using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Bookkeeping.Domain.Identity;
using Bookkeeping.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;

namespace Bookkeeping.Infrastructure.Persistence.Configurations
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            await SeedRoles(roleManager);

            await SeedRoleClaims(roleManager);

        }

        private static async Task SeedRoles(RoleManager<IdentityRole<Guid>> roleManager)
        {
            // BusinessRole is the canonical list of roles; the Identity role catalog
            // mirrors it rather than a hand-typed string array.
            foreach (var role in Enum.GetNames<BusinessRole>())
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<Guid>(role));
                }
            }
        }

        private static async Task SeedRoleClaims(RoleManager<IdentityRole<Guid>> roleManager)
        {
            // Permissions come straight from the canonical role -> permission map, so
            // the seeded claims can never drift from what each role is defined to allow.
            foreach (var (role, permissions) in RolePermissions.Map)
            {
                foreach (var permission in permissions)
                {
                    await AddPermission(roleManager, role.ToString(), permission);
                }
            }
        }

        private static async Task AddPermission(
        RoleManager<IdentityRole<Guid>> roleManager,
        string roleName,
        string permission)
        {
            var role = await roleManager.FindByNameAsync(roleName);

            if (role == null)
                return;

            var claims = await roleManager.GetClaimsAsync(role);

            if (!claims.Any(c =>
                c.Type == BookkeepingClaims.Permission &&
                c.Value == permission))
            {
                await roleManager.AddClaimAsync(
                    role,
                    new Claim(BookkeepingClaims.Permission, permission));
            }
        }

    }
}