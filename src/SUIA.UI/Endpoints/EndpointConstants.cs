namespace SUIA.UI.Endpoints;

public class EndpointConstants
{
    // Identity
    //public const string REGISTER = "api/identity/register";
    //public const string LOGIN = "api/identity/login";
    public const string REFRESH_TOKEN = "api/identity/refresh";
    public const string GET_INFO = "api/identity/manage/info";

    // Users
    public const string LOGOUT = "api/users/logout";
    public const string GET_CLAIMS = "api/users/claims";
    public const string GET_USERS = "api/users";
    public const string GET_USER = "api/users/{0}";
    public const string REGISTER = "api/users/auth/register";
    public const string LOGIN = "api/users/auth/login";

    // Roles
    public const string GET_ROLES = "api/roles";
    public const string GET_ROLE = "api/roles/{0}";
    public const string POST_ROLE = "api/roles";
}