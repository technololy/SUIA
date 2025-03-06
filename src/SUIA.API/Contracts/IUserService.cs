using Microsoft.AspNetCore.Identity;
using SUIA.Shared.Models;
using System.Security.Claims;
using SUIA.API.Data;

namespace SUIA.API.Contracts;
public interface IUserService : IServices
{
    ValueTask<bool> DeleteById(string id, CancellationToken cancellationToken);
    ValueTask<ApplicationUser[]> GetAll(CancellationToken cancellationToken);
    ValueTask<ApplicationUser?> GetById(string id, CancellationToken cancellationToken);
    ValueTask<string?> GetUserClaims(ClaimsPrincipal claimsPrincipal, CancellationToken cancellationToken);
    ValueTask<bool> UpdateById(string id, UserDto userDto, CancellationToken cancellationToken);
}