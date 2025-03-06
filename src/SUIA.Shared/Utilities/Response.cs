using SUIA.Shared.Models;

namespace SUIA.Shared.Utilities;

public static class Response
{
    public static ApiResults<T> CreateSuccessResult<T>(T data, string message = "Success")
    {
        return new ApiResults<T>(System.Net.HttpStatusCode.OK, data, message);
    }

    public static ApiResults<T> CreateFailureResult<T>(string message)
    {
        return new ApiResults<T>(System.Net.HttpStatusCode.BadRequest, default, message);
    }
    
    // ✅ Overload for default failures (defaults to `bool`)
    public static ApiResults<bool> CreateFailureResult(string message)
    {
        return new ApiResults<bool>(System.Net.HttpStatusCode.BadRequest, false, message);
    }
}