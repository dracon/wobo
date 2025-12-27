using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using woboapi.Models;

namespace woboapi.Tests;

public class AuthControllerIntegrationTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory<Program> _factory;

    public AuthControllerIntegrationTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_ReturnsToken_WhenCredentialsAreValid()
    {
        // Arrange - First create a user
        var registerUser = new UserModel("Test User", "test@example.com", "Password123")
        {
            Gender = Gender.male
        };
        await _client.PostAsJsonAsync("/api/User", registerUser);

        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResponse);
        Assert.NotEmpty(loginResponse.Token);
        Assert.True(loginResponse.ExpiresAt > DateTime.UtcNow);
        Assert.NotNull(loginResponse.User);
        Assert.Equal("test@example.com", loginResponse.User.Email);
        Assert.Null(loginResponse.User.Password); // Password should not be returned
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenEmailDoesNotExist()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "nonexistent@example.com",
            Password = "Password123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password", content);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordIsIncorrect()
    {
        // Arrange - First create a user
        var registerUser = new UserModel("Test User", "test@example.com", "Password123")
        {
            Gender = Gender.male
        };
        await _client.PostAsJsonAsync("/api/User", registerUser);

        var loginRequest = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Invalid email or password", content);
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenEmailIsMissing()
    {
        // Arrange
        var loginRequest = new { Password = "Password123" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenPasswordIsMissing()
    {
        // Arrange
        var loginRequest = new { Email = "test@example.com" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ReturnsBadRequest_WhenEmailIsInvalid()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "invalid-email",
            Password = "Password123"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
