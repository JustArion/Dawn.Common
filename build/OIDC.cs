using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

public static class OIDC
{
    private static readonly HttpClient _client = new();
    private const string AUDIENCE = "nuget.org";

    public static async Task<string> Create(string userName)
    {
        // GitHub populates these when we have the 'id-token: write' permission
        var requestToken = Environment.GetEnvironmentVariable("ACTIONS_ID_TOKEN_REQUEST_TOKEN");
        var requestUrl = Environment.GetEnvironmentVariable("ACTIONS_ID_TOKEN_REQUEST_URL");

        if (string.IsNullOrWhiteSpace(requestToken) || string.IsNullOrWhiteSpace(requestUrl))
            throw new NotSupportedException("OIDC tokens are only issued by workflows that have the `id-token: write` permission granted.");

        using var oidcRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{requestUrl}&audience={Uri.EscapeDataString(AUDIENCE)}");
        oidcRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", requestToken);

        using var oidcResponse = await _client.SendAsync(oidcRequest);
        oidcResponse.EnsureSuccessStatusCode();
        var oidc = await oidcResponse.Content.ReadFromJsonAsync<OidcResponse>();

        using var nugetRequest = new HttpRequestMessage(HttpMethod.Post, "https://www.nuget.org/api/v2/token");
        nugetRequest.Content = JsonContent.Create(new NugetTokenRequest(userName), options: JsonSerializerOptions.Web);
        nugetRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oidc.Value);

        using var nugetResponse = await _client.SendAsync(nugetRequest);
        if (!nugetResponse.IsSuccessStatusCode)
        {
            // The body usually says why NuGet rejected it
            var body = await nugetResponse.Content.ReadAsStringAsync();
            throw new HttpRequestException($"NuGet token exchange failed ({(int)nugetResponse.StatusCode}): {body}");
        }

        var result = await nugetResponse.Content.ReadFromJsonAsync<NugetTokenResponse>();
        return result.ApiKey;
    }

    private record OidcResponse(string Value);
    private record NugetTokenRequest(string Username, string TokenType = "ApiKey");
    private record NugetTokenResponse(string ApiKey);
}