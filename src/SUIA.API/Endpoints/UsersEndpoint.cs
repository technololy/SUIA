using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SUIA.API.Contracts;
using SUIA.API.Data;
using SUIA.Shared.Models;
using SUIA.Shared.Utilities;

namespace SUIA.API.Endpoints;

public sealed class UsersEndpoint(IUserService service) : IEndpoints
{
    public void Register(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/users").WithTags("Users").RequireAuthorization();
        group.MapGet("/{id}", GetUser);
        group.MapGet("/", GetAllUsers);
        group.MapGet("/claims", GetUserClaims);
        group.MapGet("/logout", LogoutUser);

        group.MapPost("/", CreateUser);

        group.MapPut("/{id}", UpdateUser);
        group.MapPut("/changePassword/{id}", UpdatePassword);

        group.MapDelete("/{id}", DeleteUser);
        
        // ✅ Authentication Endpoints
        group.MapPost("/auth/login", LoginUser).AllowAnonymous();
        group.MapPost("/auth/register", RegisterUser).AllowAnonymous();
    }

    private async Task<IResult> GetAllUsers(CancellationToken cancellationToken)
    {
        var users = await service.GetAll(cancellationToken);
        return Results.Ok(Response.CreateSuccessResult(users, "Users retrieved successfully"));
    }

    private async Task<IResult> GetUser(string id, CancellationToken cancellationToken)
    {
        var user = await service.GetById(id, cancellationToken);
        if (user is null) return Results.NotFound(Response.CreateFailureResult<bool>("User not found"));
        return Results.Ok(Response.CreateSuccessResult(user, "User retrieved successfully"));
    }

    private async Task<IResult> GetUserClaims(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var claims = await service.GetUserClaims(user, cancellationToken);
        if (string.IsNullOrEmpty(claims)) return Results.Unauthorized();
        return Results.Ok(Response.CreateSuccessResult(claims, "Claims retrieved successfully"));
    }

    private async Task<IResult> LogoutUser(SignInManager<ApplicationUser> signInManager, CancellationToken cancellationToken)
    {
        await signInManager.SignOutAsync();
        return Results.Ok(Response.CreateSuccessResult(true, "User logged out successfully"));
    }

    private async Task CreateUser(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    private async Task<IResult> UpdateUser(string id, UserDto model, CancellationToken cancellationToken)
    {
        var result = await service.UpdateById(id, model, cancellationToken);
        if (!result) return Results.BadRequest(Response.CreateFailureResult("Failed to update user"));
        return Results.Ok(Response.CreateSuccessResult(result, "User updated successfully"));
    }

    private async Task<IResult> UpdatePassword(string id, ChangePasswordRequestDto request, ApplicationDbContext adbc, UserManager<ApplicationUser> userManager, CancellationToken cancellationToken)
    {
        var user = await adbc.Users.FindAsync([id], cancellationToken: cancellationToken);
        if (user is null) return Results.NotFound(Response.CreateFailureResult("User not found"));

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (result.Errors.Any()) 
            return Results.Problem(Response.CreateFailureResult(result.Errors.First().Description).Message, statusCode: 400, title: "Change password failed");
        
        return Results.Ok(Response.CreateSuccessResult(true, "Password updated successfully"));
    }

    private async Task<IResult> DeleteUser(string id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteById(id, cancellationToken);
        if (!result) return Results.BadRequest(Response.CreateFailureResult("Failed to delete user"));
        return Results.Ok(Response.CreateSuccessResult(result, "User deleted successfully"));
    }

    private static async Task<IResult> LoginUser(
        [FromBody] LoginRequestDto model,
        [FromServices] UserManager<ApplicationUser> userManager,
        [FromServices] SignInManager<ApplicationUser> signInManager,
        [FromServices] IConfiguration config,
        HttpContext httpContext)    
    {
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null) return Results.BadRequest(Response.CreateFailureResult("Invalid email or password"));

        var result = await signInManager.PasswordSignInAsync(user, model.Password, false, false);
        if (!result.Succeeded) return Results.BadRequest(Response.CreateFailureResult("Invalid login attempt"));

        // ✅ Retrieve user roles
        var roles = await userManager.GetRolesAsync(user);
        // ✅ Retrieve claims
        var userClaims = await userManager.GetClaimsAsync(user);
        var claimsList = userClaims.Select(c => $"{c.Type}:{c.Value}").ToList();
        // ✅ Add role claims
        foreach (var role in roles)
        {
            userClaims.Add(new Claim(ClaimTypes.Role, role));  // ✅ Add roles to claims
            claimsList.Add($"{ClaimTypes.Role}:{role}");
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var apiHost = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

        var token = new JwtSecurityToken(
            issuer: apiHost,
            audience: apiHost,
            claims: userClaims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var response = new LoginResponseDto("Bearer", tokenString, 3600, "refresh_token_placeholder")
        {
            Claims = string.Join(",", claimsList)
        };

        return Results.Ok(Response.CreateSuccessResult(response, "Login successful"));
    }

    private async Task<IResult> RegisterUser(
        [FromBody] RegisterModel model,
        [FromServices] UserManager<ApplicationUser> userManager)
    {
        var user = new ApplicationUser
        {
            UserName = model.Username,
            Email = model.Email,
            Gender = model.Gender ?? "NA",
            FirstName = model.FirstName,
            LastName = model.LastName,
        };

        var result = await userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded) return Results.BadRequest(Response.CreateFailureResult("User registration failed"));

        // Add user claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new("Gender", user.Gender) // Custom claim
        };

        await userManager.AddClaimsAsync(user, claims);

        return Results.Ok(Response.CreateSuccessResult(true, "User registered successfully"));
    }


}