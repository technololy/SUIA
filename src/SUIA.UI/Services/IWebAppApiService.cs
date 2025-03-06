using SUIA.Shared.Models;

namespace SUIA.UI.Services;

public interface IWebAppApiService
{
    ValueTask<ApiResults<TOutput>> GetAsync<TOutput>(string endpoints, CancellationToken cancellationToken = default);
    ValueTask<ApiResults<TOutput>> PostAsync<TInput, TOutput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default);
    ValueTask<ApiResults<TOutput>> PutAsync<TInput, TOutput>(string endpoints, TInput requestBody, CancellationToken cancellationToken = default);
    ValueTask<ApiResults<TOutput>> DeleteAsync<TOutput>(string endpoints, CancellationToken cancellationToken = default);
}

public interface IEmpty;