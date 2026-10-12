using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SchoolERP.Shared.Models;
using Xunit;

namespace SchoolERP.IntegrationTests;

public static class HttpTestExtensions
{
    public static HttpClient WithBearerToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static HttpClient WithCorrelationId(this HttpClient client, string correlationId)
    {
        client.DefaultRequestHeaders.Remove("X-Correlation-ID");
        client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
        return client;
    }

    public static async Task<ApiErrorResponse> ShouldBeApiErrorAsync(this HttpResponseMessage response,
        HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();
        Assert.NotNull(error);
        Assert.False(error.Success);
        Assert.Equal(code, error.Code);
        Assert.Equal((int)status, error.Status);
        Assert.False(string.IsNullOrWhiteSpace(error.CorrelationId));
        return error;
    }
}
