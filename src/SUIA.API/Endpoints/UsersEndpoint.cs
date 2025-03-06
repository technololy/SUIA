using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SUIA.API.Contracts;
using SUIA.API.Data;
using SUIA.Shared.Models;

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
        // New Authentication Endpoints
        group.MapPost("/auth/login", LoginUser).AllowAnonymous();
        group.MapPost("/auth/register", RegisterUser).AllowAnonymous();
    }

    private async Task<IResult> GetAllUsers(CancellationToken cancellationToken)
        => Results.Ok(await service.GetAll(cancellationToken));

    private async Task<IResult> GetUser(string id, CancellationToken cancellationToken)
    {
        var user = await service.GetById(id, cancellationToken);        
        if (user is null) return Results.NotFound();
        return Results.Ok(user);
    }

    private async Task<IResult> GetUserClaims(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var claims = await service.GetUserClaims(user, cancellationToken);
        if (string.IsNullOrEmpty(claims)) return Results.Unauthorized();
        return Results.Content(claims);        
    }

    private async Task<IResult> LogoutUser(SignInManager<ApplicationUser> signInManager, CancellationToken cancellationToken)
    {
        await signInManager.SignOutAsync();
        return Results.Ok();
    }

    private async Task CreateUser(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    private async Task<IResult> UpdateUser(string id, UserDto model, CancellationToken cancellationToken)
    {
        var result = await service.UpdateById(id, model, cancellationToken);
        if (result) return Results.NoContent();
        return Results.BadRequest("Failed to update user.");
    }

    private async Task<IResult> UpdatePassword(string id, ChangePasswordRequestDto request, ApplicationDbContext adbc, UserManager<ApplicationUser> userManager, CancellationToken cancellationToken)
    {
        var user = await adbc.Users.FindAsync([id], cancellationToken: cancellationToken);
        if (user is null) return Results.NotFound();        
        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (result.Errors.Any()) return Results.Problem(result.Errors.First().Description, statusCode: 400, title: "Change password failed");
        return Results.NoContent();
    }

    private async Task<IResult> DeleteUser(string id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteById(id, cancellationToken);
        if (result) return Results.NoContent();
        return Results.BadRequest("Failed to delete user.");
    }
    private static async Task<IResult> LoginUser(
        [FromBody] LoginRequestDto model,
        [FromServices] UserManager<ApplicationUser> userManager,
        [FromServices] SignInManager<ApplicationUser> signInManager,
        [FromServices] IConfiguration config,
        HttpContext httpContext)    
    {
        var user = await userManager.FindByEmailAsync(model.Email);
        if (user is null) return Results.BadRequest(new { Message = "Invalid email or password." });

        var result = await signInManager.PasswordSignInAsync(user, model.Password, false, false);
        if (!result.Succeeded) return Results.BadRequest(new { Message = "Invalid login attempt." });

        // ✅ Retrieve claims
        var userClaims = await userManager.GetClaimsAsync(user);
        var claimsList = userClaims.Select(c => $"{c.Type}:{c.Value}").ToList();

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var apiHost = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";

        var token = new JwtSecurityToken(
            //issuer: config["Jwt:Issuer"],
            issuer: apiHost,
            //audience: config["Jwt:Audience"],
            audience: apiHost,
            claims: userClaims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        var response = new LoginResponseDto("Bearer", tokenString, 3600, "refresh_token_placeholder")
        {
            Claims = string.Join(",", claimsList) // ✅ Send claims as a string
        };

        return Results.Ok(CreateSuccessResult(response, "Login successful."));
    }
    private static ApiResults<T> CreateSuccessResult<T>(T data, string message = "Success")
    {
        return new ApiResults<T>(System.Net.HttpStatusCode.OK, data, message);
    }

    private static ApiResults<T> CreateFailureResult<T>(string message)
    {
        return new ApiResults<T>(System.Net.HttpStatusCode.BadRequest, default, message);
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
        if (!result.Succeeded) return Results.BadRequest(result.Errors);

        // Add user claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new("Gender", user.Gender) // Custom claim
        };

        await userManager.AddClaimsAsync(user, claims);

        return Results.Ok(new { Message = "User registered successfully" });
    }
}
