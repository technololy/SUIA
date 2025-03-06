using System.Net;

namespace SUIA.Shared.Models;

public record ApiResults<TOutput>(HttpStatusCode StatusCode, TOutput? Data, string? Message, string? StringValue = null)
{
    public bool IsSuccess => ((int)StatusCode) >= 200 && ((int)StatusCode) < 300;
    public ValidationProblem? Errors { get; set; }
};