using System.Security.Claims;
using SUIA.Shared.Models;
using System.Text;

namespace SUIA.Shared.Utilities;
public static class UserExtension
{
    public static UserClaimsDto? FromRawClaims(this string claims)
    {
        var claimsModel = claims.FromJson<LoginResponseDto>();
        if (claimsModel.Claims is null) return default;
        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(claimsModel.Claims!)).FromJson<UserClaimsDto>();
        }
        catch
        {
            return default;
        }
    }
    
    public static UserClaimsDto? FromRawClaimsAsSchema(this string claims)
    {
        if (string.IsNullOrWhiteSpace(claims)) return null;

        try
        {
            var claimPairs = claims.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(claim =>
                {
                    var index = claim.LastIndexOf(':'); // ✅ Get the LAST colon to avoid breaking the key
                    return index > 0 
                        ? new { Key = claim[..index].Trim(), Value = claim[(index + 1)..].Trim() } 
                        : null;
                })
                .Where(pair => pair != null)
                .ToDictionary(pair => pair!.Key, pair => pair!.Value);

            // ✅ Extract required fields
            if (!claimPairs.TryGetValue(ClaimTypes.NameIdentifier, out var id) ||
                !claimPairs.TryGetValue(ClaimTypes.Name, out var userName) ||
                !claimPairs.TryGetValue(ClaimTypes.Email, out var email))
            {
                return null; // Essential fields are missing
            }

            // ✅ Extract multiple roles properly (roles are usually stored as a semicolon-separated string)
            var roles = claimPairs.TryGetValue(ClaimTypes.Role, out var value)
                ? value.Split(';').ToList()  // ✅ Convert to List<string>
                : [];
            
            return new UserClaimsDto(id, userName, email, roles);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing claims: {ex.Message}");
            return null;
        }
    }
}
