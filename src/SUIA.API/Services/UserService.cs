using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SUIA.API.Contracts;
using SUIA.API.Data;
using SUIA.Shared.Models;
using SUIA.Shared.Utilities;
using System.Security.Claims;
using System.Text;

namespace SUIA.API.Services;



public class UserService(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager) : IUserService
{
    public async ValueTask<ApplicationUser[]> GetAll(CancellationToken cancellationToken)
        => await dbContext.Users.ToArrayAsync(cancellationToken) ?? [];

    public async ValueTask<ApplicationUser?> GetById(string id, CancellationToken cancellationToken)
        => await dbContext.Users.FindAsync([id], cancellationToken: cancellationToken);

    public async ValueTask<string?> GetUserClaims(ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken)
    {
        var validUser = await userManager.GetUserAsync(claimsPrincipal);
        if (validUser is null) return null;

        // ✅ Ensure roles are stored as a List<string>
        var roles = new List<string>();
        if (validUser.Email!.StartsWith("admin@"))
        {
            roles.Add("Admin");
        }
        else
        {
            roles.Add("User");
        }

        var claims = new UserClaimsDto(
            validUser.Id,
            validUser.UserName!,
            validUser.Email!,
            roles // ✅ Now passing a List<string> instead of a string
        );

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(claims.ToJson()));
    }
    public async ValueTask<bool> UpdateById(string id, UserDto userDto, CancellationToken cancellationToken)
    {
        var existingUser = await dbContext.Users.FindAsync([id], cancellationToken: cancellationToken);
        if (existingUser is null) return false;
        existingUser.EmailConfirmed = userDto.EmailConfirmed;
        existingUser.PhoneNumberConfirmed = userDto.PhoneNumberConfirmed;
        existingUser.TwoFactorEnabled = userDto.TwoFactorEnabled;
        existingUser.LockoutEnabled = userDto.LockoutEnabled;
        dbContext.Update(existingUser);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
    public async ValueTask<bool> DeleteById(string id, CancellationToken cancellationToken)
    {
        var existingUser = await dbContext.Users.FindAsync([id], cancellationToken: cancellationToken);
        if (existingUser is null) return false;
        dbContext.Remove(existingUser);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
