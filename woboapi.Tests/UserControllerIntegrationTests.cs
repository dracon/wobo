using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using woboapi.Models;

namespace woboapi.Tests;

public class UserControllerIntegrationTests : IClassFixture<TestWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory<Program> _factory;

    public UserControllerIntegrationTests(TestWebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetAuthTokenAsync()
    {
        // Create a user
        var registerUser = new UserModel("Auth User", "auth@example.com", "Password123")
        {
            Gender = Gender.male
        };
        await _client.PostAsJsonAsync("/api/User", registerUser);

        // Login to get token
        var loginRequest = new LoginRequest
        {
            Email = "auth@example.com",
            Password = "Password123"
        };
        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", loginRequest);
        var loginResult = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        return loginResult!.Token;
    }

    #region POST /api/User (Create User - Public)

    [Fact]
    public async Task CreateUser_ReturnsCreated_WhenDataIsValid()
    {
        // Arrange
        var user = new UserModel("John Doe", "john@example.com", "Password123")
        {
            Gender = Gender.male
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/User", user);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdUser = await response.Content.ReadFromJsonAsync<UserModel>();
        Assert.NotNull(createdUser);
        Assert.NotEqual(Guid.Empty, createdUser.Id);
        Assert.Equal("John Doe", createdUser.Name);
        Assert.Equal("john@example.com", createdUser.Email);
        Assert.Null(createdUser.Password); // Password should not be returned
        Assert.Equal(Gender.male, createdUser.Gender);
    }

    [Fact]
    public async Task CreateUser_ReturnsBadRequest_WhenEmailIsInvalid()
    {
        // Arrange
        var user = new UserModel("John Doe", "invalid-email", "Password123")
        {
            Gender = Gender.male
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/User", user);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ReturnsBadRequest_WhenPasswordIsTooShort()
    {
        // Arrange
        var user = new UserModel("John Doe", "john@example.com", "Short1")
        {
            Gender = Gender.male
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/User", user);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ReturnsConflict_WhenEmailAlreadyExists()
    {
        // Arrange
        var user1 = new UserModel("John Doe", "duplicate@example.com", "Password123")
        {
            Gender = Gender.male
        };
        await _client.PostAsJsonAsync("/api/User", user1);

        var user2 = new UserModel("Jane Doe", "duplicate@example.com", "Password456")
        {
            Gender = Gender.female
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/User", user2);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("duplicate@example.com", content);
    }

    #endregion

    #region GET /api/User (Get All Users - Requires Auth)

    [Fact]
    public async Task GetAllUsers_ReturnsUnauthorized_WhenNoToken()
    {
        // Act
        var response = await _client.GetAsync("/api/User");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllUsers_ReturnsOk_WhenAuthenticated()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create a few users
        var user1 = new UserModel("User1", "user1@example.com", "Password123")
        {
            Gender = Gender.male
        };
        var user2 = new UserModel("User2", "user2@example.com", "Password123")
        {
            Gender = Gender.female
        };
        await _client.PostAsJsonAsync("/api/User", user1);
        await _client.PostAsJsonAsync("/api/User", user2);

        // Act
        var response = await _client.GetAsync("/api/User");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var users = await response.Content.ReadFromJsonAsync<List<UserModel>>();
        Assert.NotNull(users);
        Assert.True(users.Count >= 3); // At least the auth user + 2 created
        Assert.All(users, u => Assert.Null(u.Password)); // No passwords should be returned
    }

    #endregion

    #region GET /api/User/{id} (Get User By Id - Requires Auth)

    [Fact]
    public async Task GetUserById_ReturnsUnauthorized_WhenNoToken()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/User/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_ReturnsOk_WhenUserExists()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newUser = new UserModel("Test User", "gettest@example.com", "Password123")
        {
            Gender = Gender.neutral
        };
        var createResponse = await _client.PostAsJsonAsync("/api/User", newUser);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserModel>();

        // Act
        var response = await _client.GetAsync($"/api/User/{createdUser!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<UserModel>();
        Assert.NotNull(user);
        Assert.Equal(createdUser.Id, user.Id);
        Assert.Equal("Test User", user.Name);
        Assert.Equal("gettest@example.com", user.Email);
        Assert.Null(user.Password);
    }

    [Fact]
    public async Task GetUserById_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/User/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion

    #region PUT /api/User/{id} (Update User - Requires Auth)

    [Fact]
    public async Task UpdateUser_ReturnsUnauthorized_WhenNoToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updateData = new UserModel("Updated", "updated@example.com", "Password123")
        {
            Gender = Gender.male
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/User/{userId}", updateData);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ReturnsNoContent_WhenUpdateIsSuccessful()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newUser = new UserModel("Original Name", "original@example.com", "Password123")
        {
            Gender = Gender.male
        };
        var createResponse = await _client.PostAsJsonAsync("/api/User", newUser);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserModel>();

        var updateData = new UserModel("Updated Name", "updated@example.com", "NewPassword123")
        {
            Gender = Gender.neutral
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/User/{createdUser!.Id}", updateData);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the update
        var getResponse = await _client.GetAsync($"/api/User/{createdUser.Id}");
        var updatedUser = await getResponse.Content.ReadFromJsonAsync<UserModel>();
        Assert.Equal("Updated Name", updatedUser!.Name);
        Assert.Equal("updated@example.com", updatedUser.Email);
        Assert.Equal(Gender.neutral, updatedUser.Gender);
    }

    [Fact]
    public async Task UpdateUser_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var nonExistentId = Guid.NewGuid();
        var updateData = new UserModel("Updated", "updated@example.com", "Password123")
        {
            Gender = Gender.male
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/User/{nonExistentId}", updateData);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ReturnsConflict_WhenEmailAlreadyExistsForDifferentUser()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var user1 = new UserModel("User1", "user1conflict@example.com", "Password123")
        {
            Gender = Gender.male
        };
        var user2 = new UserModel("User2", "user2conflict@example.com", "Password123")
        {
            Gender = Gender.female
        };

        var response1 = await _client.PostAsJsonAsync("/api/User", user1);
        var response2 = await _client.PostAsJsonAsync("/api/User", user2);
        var createdUser2 = await response2.Content.ReadFromJsonAsync<UserModel>();

        // Try to update user2 with user1's email
        var updateData = new UserModel("User2 Updated", "user1conflict@example.com", "Password123")
        {
            Gender = Gender.female
        };

        // Act
        var response = await _client.PutAsJsonAsync($"/api/User/{createdUser2!.Id}", updateData);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    #endregion

    #region DELETE /api/User/{id} (Delete User - Requires Auth)

    [Fact]
    public async Task DeleteUser_ReturnsUnauthorized_WhenNoToken()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/User/{userId}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_ReturnsNoContent_WhenDeleteIsSuccessful()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newUser = new UserModel("To Delete", "delete@example.com", "Password123")
        {
            Gender = Gender.male
        };
        var createResponse = await _client.PostAsJsonAsync("/api/User", newUser);
        var createdUser = await createResponse.Content.ReadFromJsonAsync<UserModel>();

        // Act
        var response = await _client.DeleteAsync($"/api/User/{createdUser!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the user is deleted
        var getResponse = await _client.GetAsync($"/api/User/{createdUser.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        var token = await GetAuthTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/User/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    #endregion
}
