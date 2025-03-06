using SUIA.API.Contracts;
using SUIA.Shared.Models;
using SUIA.Shared.Utilities;

namespace SUIA.API.Endpoints;

public sealed class RolesEndpoint(IRoleService service) : IEndpoints
{
    public void Register(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/roles").WithTags("Roles").RequireAuthorization();
        group.MapGet("/{id}", GetRole);
        group.MapGet("/", GetAllRoles);
        group.MapPost("/", CreateRole);
        group.MapPut("/{id}", UpdateRole);
        group.MapDelete("/{id}", DeleteRole);
    }

    private async Task<IResult> GetAllRoles(CancellationToken cancellationToken)
    {
        var roles = await service.GetAll(cancellationToken);
        return Results.Ok(Response.CreateSuccessResult(roles, "Roles retrieved successfully"));
    }

    private async Task<IResult> GetRole(string id, CancellationToken cancellationToken)
    {        
        var existingRole = await service.GetById(id, cancellationToken);
        if (existingRole is null) return Results.NotFound(Response.CreateFailureResult<bool>("Role not found"));
        return Results.Ok(Response.CreateSuccessResult(existingRole, "Role retrieved successfully"));
    }

    private async Task<IResult> CreateRole(RolesDto rolesDto, CancellationToken cancellationToken)
    {
        var result = await service.Create(rolesDto, cancellationToken);
        if (result is null) return Results.BadRequest(Response.CreateFailureResult("Failed to create role"));
        return Results.Ok(Response.CreateSuccessResult(result, "Role created successfully"));
    }

    private async Task<IResult> UpdateRole(string id, RolesDto rolesDto, CancellationToken cancellationToken)
    {
        var result = await service.UpdateById(id, rolesDto, cancellationToken);
        if (!result) return Results.BadRequest(Response.CreateFailureResult("Failed to update role"));
        return Results.Ok(Response.CreateSuccessResult(result, "Role updated successfully"));
    }

    private async Task<IResult> DeleteRole(string id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteById(id, cancellationToken);
        if (!result) return Results.BadRequest(Response.CreateFailureResult("Failed to delete role"));
        return Results.Ok(Response.CreateSuccessResult(result, "Role deleted successfully"));
    }
}