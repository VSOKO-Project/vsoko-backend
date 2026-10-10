using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Common.ResultsDto;
using Presentation.Common.DTOs;

namespace Api.IntegrationTests.Support;

public static class ApiClient
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<LoginResultDto> LoginAsync(this ApiFactory factory, string login, string password)
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/Security/Login", new { login, password });
        return await response.DataAsync<LoginResultDto>();
    }

    public static HttpClient ClientWithToken(this ApiFactory factory, string? token)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public static async Task<HttpClient> LoginClientAsync(this ApiFactory factory, string login, string password) =>
        factory.ClientWithToken((await factory.LoginAsync(login, password)).AccessToken);

    public static Task<HttpClient> AdminAsync(this ApiFactory factory) =>
        factory.LoginClientAsync(ApiFactory.AdminLogin, ApiFactory.AdminPassword);

    public static async Task<T> DataAsync<T>(this HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"{(int)response.StatusCode} {response.RequestMessage?.RequestUri}: {body}");
        var result = JsonSerializer.Deserialize<ApiSuccessResult<T>>(body, Json)!;
        return result.Data!;
    }

    public static async Task<JsonElement> ProblemAsync(this HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"{(int)response.StatusCode} {response.RequestMessage?.RequestUri}: {body}");
        return JsonSerializer.Deserialize<JsonElement>(body);
    }
}
