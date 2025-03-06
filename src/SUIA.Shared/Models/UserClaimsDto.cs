namespace SUIA.Shared.Models;

public record UserClaimsDto(string Id, string UserName, string Email, List<string> Roles);