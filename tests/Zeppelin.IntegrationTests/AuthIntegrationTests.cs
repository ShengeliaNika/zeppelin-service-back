using System.Net;
using System.Net.Http.Json;

namespace Zeppelin.IntegrationTests;

public class AuthIntegrationTests(ZeppelinApiFactory factory) : IClassFixture<ZeppelinApiFactory>
{
    private record LoginRequest(string Email, string Password);
    private record RefreshRequest(string RefreshToken);
    private record UserSummary(string Id, string Email, string FirstName, string LastName, string[] Roles);
    private record AuthResponse(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RefreshToken, UserSummary User);

    [Fact]
    public async Task Login_WithSeededAdmin_ReturnsTokenAndAdminRole()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(ZeppelinApiFactory.AdminEmail, ZeppelinApiFactory.AdminPassword));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrEmpty(body!.AccessToken));
        Assert.Contains("Admin", body.User.Roles);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(ZeppelinApiFactory.AdminEmail, "WrongPassword123!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminOnlyEndpoint_WithNonAdminRole_ReturnsForbidden()
    {
        var adminClient = factory.CreateClient();
        var adminLogin = await adminClient.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(ZeppelinApiFactory.AdminEmail, ZeppelinApiFactory.AdminPassword));
        var adminAuth = await adminLogin.Content.ReadFromJsonAsync<AuthResponse>();

        adminClient.DefaultRequestHeaders.Authorization = new("Bearer", adminAuth!.AccessToken);
        var createUserResponse = await adminClient.PostAsJsonAsync("/api/users", new
        {
            Email = "dentist@test.local",
            Password = "DentistTest123!",
            FirstName = "Test",
            LastName = "Dentist",
            Roles = new[] { "Dentist" },
        });
        Assert.Equal(HttpStatusCode.Created, createUserResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Created, createUserResponse.StatusCode);

        var dentistClient = factory.CreateClient();
        var dentistLogin = await dentistClient.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("dentist@test.local", "DentistTest123!"));
        var dentistAuth = await dentistLogin.Content.ReadFromJsonAsync<AuthResponse>();

        dentistClient.DefaultRequestHeaders.Authorization = new("Bearer", dentistAuth!.AccessToken);
        var response = await dentistClient.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatesToken_AndOldTokenBecomesInvalid()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(ZeppelinApiFactory.AdminEmail, ZeppelinApiFactory.AdminPassword));
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(auth!.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);

        var reuseResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(auth.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }
}
