using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

public static class OIDC
{
    private const string AUDIENCE = "https://www.nuget.org";
    public static async Task<string> Create(string userName)
    {
        // GitHub populates these when we have the 'id-token: write' permission
        var oidcToken = Environment.GetEnvironmentVariable("ACTIONS_ID_TOKEN_REQUEST_TOKEN");
        var oidcRequestUrl = Environment.GetEnvironmentVariable("ACTIONS_ID_TOKEN_REQUEST_URL");

        if (string.IsNullOrWhiteSpace(oidcToken) || string.IsNullOrWhiteSpace(oidcRequestUrl))
            throw new NotSupportedException("OIDC tokens are only issued by workflows that have the `id-token: write` permission granted, this is either not a cloud workflow, or the permission has not been granted");

        // request the token
        var tokenUrl = $"{oidcRequestUrl}&audience={Uri.EscapeDataString(AUDIENCE)}";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", oidcToken);

        var response = await client.GetFromJsonAsync<OidcResponse>(tokenUrl);
        var token = response.Value;
        
        // request nuget api key with token
        var response2 = await client.PostAsJsonAsync("https://www.nuget.org/api/v2/token", new NugetTokenRequest(userName));
        response2.EnsureSuccessStatusCode();
        var responseBody = await response2.Content.ReadFromJsonAsync<NugetTokenResponse>();

        return responseBody.ApiKey;
    }

    private record OidcResponse(string Value);

    private record NugetTokenRequest(string Username, string TokenType = "ApiKey");
    private record NugetTokenResponse(string ApiKey);
}