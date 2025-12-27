using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using woboapi;
using woboapi.Data;
using woboapi.Exceptions;
using woboapi.Models;
using woboapi.Services;

namespace woboapi.Tests;

public class UserServiceTests
{
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly Mock<IPasswordService> _mockPasswordService;
    private readonly ApplicationDbContext _context;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Setup mocks
        _mockLogger = new Mock<ILogger<UserService>>();
        _mockPasswordService = new Mock<IPasswordService>();

        // Create service instance
        _userService = new UserService(_mockLogger.Object, _context, _mockPasswordService.Object);
    }

    #region GetAllUsers Tests

    [Fact]
    public void GetAllUsers_ReturnsEmptyList_WhenNoUsers()
    {
        // Act
        var result = _userService.GetAllUsers();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetAllUsers_ReturnsAllUsers_WhenUsersExist()
    {
        // Arrange
        var user1 = new UserModel("John Doe", "john@test.com", "hashedPassword1")
        {
            Id = Guid.NewGuid(),
            Gender = Gender.male,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var user2 = new UserModel("Jane Doe", "jane@test.com", "hashedPassword2")
        {
            Id = Guid.NewGuid(),
            Gender = Gender.female,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(user1, user2);
        _context.SaveChanges();

        // Act
        var result = _userService.GetAllUsers();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, user => Assert.Null(user.Password)); // Password should be cleared
    }

    #endregion

    #region GetUser Tests

    [Fact]
    public void GetUser_ReturnsUser_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserModel("John Doe", "john@test.com", "hashedPassword")
        {
            Id = userId,
            Gender = Gender.male,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        // Act
        var result = _userService.GetUser(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@test.com", result.Email);
        Assert.Null(result.Password); // Password should be cleared
    }

    [Fact]
    public void GetUser_ThrowsUserNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        var exception = Assert.Throws<UserNotFoundException>(() => _userService.GetUser(userId));
        Assert.Contains(userId.ToString(), exception.Message);
    }

    #endregion

    #region CreateUser Tests

    [Fact]
    public void CreateUser_CreatesUserSuccessfully_WhenValidData()
    {
        // Arrange
        var user = new UserModel("John Doe", "john@test.com", "password123")
        {
            Gender = Gender.male
        };

        _mockPasswordService.Setup(p => p.HashPassword("password123"))
            .Returns("hashedPassword123");

        // Act
        _userService.CreateUser(user);

        // Assert
        // Detach and reload from database to get actual stored values
        _context.Entry(user).State = EntityState.Detached;
        var savedUser = _context.Users.FirstOrDefault(u => u.Email == "john@test.com");
        Assert.NotNull(savedUser);
        Assert.Equal("John Doe", savedUser.Name);
        Assert.Equal("john@test.com", savedUser.Email);
        Assert.Equal("hashedPassword123", savedUser.Password);
        Assert.NotEqual(Guid.Empty, savedUser.Id);
        Assert.NotEqual(default(DateTime), savedUser.CreatedAt);
        Assert.NotEqual(default(DateTime), savedUser.UpdatedAt);
    }

    [Fact]
    public void CreateUser_ThrowsDuplicateEmailException_WhenEmailAlreadyExists()
    {
        // Arrange
        var existingUser = new UserModel("Jane Doe", "duplicate@test.com", "hashedPassword")
        {
            Id = Guid.NewGuid(),
            Gender = Gender.female,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(existingUser);
        _context.SaveChanges();

        var newUser = new UserModel("John Doe", "duplicate@test.com", "password123")
        {
            Gender = Gender.male
        };

        // Act & Assert
        var exception = Assert.Throws<DuplicateEmailException>(() => _userService.CreateUser(newUser));
        Assert.Contains("duplicate@test.com", exception.Message);
    }

    [Fact]
    public void CreateUser_HashesPassword_BeforeStoringInDatabase()
    {
        // Arrange
        var user = new UserModel("John Doe", "john@test.com", "plainPassword")
        {
            Gender = Gender.male
        };

        _mockPasswordService.Setup(p => p.HashPassword("plainPassword"))
            .Returns("hashedPassword");

        // Act
        _userService.CreateUser(user);

        // Assert
        // Detach and reload from database to get actual stored values
        _context.Entry(user).State = EntityState.Detached;
        var savedUser = _context.Users.FirstOrDefault(u => u.Email == "john@test.com");
        Assert.NotNull(savedUser);
        Assert.Equal("hashedPassword", savedUser.Password);
        _mockPasswordService.Verify(p => p.HashPassword("plainPassword"), Times.Once);
    }

    #endregion

    #region UpdateUser Tests

    [Fact]
    public void UpdateUser_UpdatesUserSuccessfully_WhenValidData()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new UserModel("John Doe", "john@test.com", "oldHashedPassword")
        {
            Id = userId,
            Gender = Gender.male,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        _context.Users.Add(existingUser);
        _context.SaveChanges();

        var updatedUser = new UserModel("John Updated", "john.updated@test.com", "newPassword")
        {
            Gender = Gender.neutral
        };

        _mockPasswordService.Setup(p => p.HashPassword("newPassword"))
            .Returns("newHashedPassword");

        // Act
        _userService.UpdateUser(userId, updatedUser);

        // Assert
        var savedUser = _context.Users.Find(userId);
        Assert.NotNull(savedUser);
        Assert.Equal("John Updated", savedUser.Name);
        Assert.Equal("john.updated@test.com", savedUser.Email);
        Assert.Equal("newHashedPassword", savedUser.Password);
        Assert.Equal(Gender.neutral, savedUser.Gender);
    }

    [Fact]
    public void UpdateUser_ThrowsUserNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var updatedUser = new UserModel("John Doe", "john@test.com", "password")
        {
            Gender = Gender.male
        };

        // Act & Assert
        var exception = Assert.Throws<UserNotFoundException>(() => _userService.UpdateUser(userId, updatedUser));
        Assert.Contains(userId.ToString(), exception.Message);
    }

    [Fact]
    public void UpdateUser_ThrowsDuplicateEmailException_WhenEmailAlreadyExistsForDifferentUser()
    {
        // Arrange
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var user1 = new UserModel("John Doe", "john@test.com", "hashedPassword1")
        {
            Id = user1Id,
            Gender = Gender.male,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var user2 = new UserModel("Jane Doe", "jane@test.com", "hashedPassword2")
        {
            Id = user2Id,
            Gender = Gender.female,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(user1, user2);
        _context.SaveChanges();

        var updatedUser = new UserModel("Jane Updated", "john@test.com", "newPassword")
        {
            Gender = Gender.female
        };

        // Act & Assert
        var exception = Assert.Throws<DuplicateEmailException>(() => _userService.UpdateUser(user2Id, updatedUser));
        Assert.Contains("john@test.com", exception.Message);
    }

    [Fact]
    public void UpdateUser_AllowsSameEmail_ForSameUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var existingUser = new UserModel("John Doe", "john@test.com", "oldHashedPassword")
        {
            Id = userId,
            Gender = Gender.male,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(existingUser);
        _context.SaveChanges();

        var updatedUser = new UserModel("John Updated", "john@test.com", "newPassword")
        {
            Gender = Gender.male
        };

        _mockPasswordService.Setup(p => p.HashPassword("newPassword"))
            .Returns("newHashedPassword");

        // Act
        _userService.UpdateUser(userId, updatedUser);

        // Assert
        var savedUser = _context.Users.Find(userId);
        Assert.NotNull(savedUser);
        Assert.Equal("John Updated", savedUser.Name);
    }

    #endregion

    #region DeleteUser Tests

    [Fact]
    public void DeleteUser_DeletesUserSuccessfully_WhenUserExists()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserModel("John Doe", "john@test.com", "hashedPassword")
        {
            Id = userId,
            Gender = Gender.male,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        // Act
        _userService.DeleteUser(userId);

        // Assert
        var deletedUser = _context.Users.Find(userId);
        Assert.Null(deletedUser);
    }

    [Fact]
    public void DeleteUser_ThrowsUserNotFoundException_WhenUserDoesNotExist()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        var exception = Assert.Throws<UserNotFoundException>(() => _userService.DeleteUser(userId));
        Assert.Contains(userId.ToString(), exception.Message);
    }

    #endregion

    #region Login Tests

    [Fact]
    public void Login_ReturnsUser_WhenCredentialsAreValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new UserModel("John Doe", "john@test.com", "hashedPassword123")
        {
            Id = userId,
            Gender = Gender.male,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        _mockPasswordService.Setup(p => p.VerifyPassword("password123", "hashedPassword123"))
            .Returns(true);

        // Act
        var result = _userService.Login("john@test.com", "password123");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john@test.com", result.Email);
        Assert.Null(result.Password); // Password should be cleared
    }

    [Fact]
    public void Login_ThrowsInvalidCredentialsException_WhenEmailDoesNotExist()
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidCredentialsException>(() =>
            _userService.Login("nonexistent@test.com", "password123"));
        Assert.Contains("Invalid email or password", exception.Message);
    }

    [Fact]
    public void Login_ThrowsInvalidCredentialsException_WhenPasswordIsIncorrect()
    {
        // Arrange
        var user = new UserModel("John Doe", "john@test.com", "hashedPassword123")
        {
            Id = Guid.NewGuid(),
            Gender = Gender.male,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        _mockPasswordService.Setup(p => p.VerifyPassword("wrongPassword", "hashedPassword123"))
            .Returns(false);

        // Act & Assert
        var exception = Assert.Throws<InvalidCredentialsException>(() =>
            _userService.Login("john@test.com", "wrongPassword"));
        Assert.Contains("Invalid email or password", exception.Message);
    }

    [Fact]
    public void Login_VerifiesPasswordUsingPasswordService()
    {
        // Arrange
        var user = new UserModel("John Doe", "john@test.com", "hashedPassword123")
        {
            Id = Guid.NewGuid(),
            Gender = Gender.male,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        _context.SaveChanges();

        _mockPasswordService.Setup(p => p.VerifyPassword("password123", "hashedPassword123"))
            .Returns(true);

        // Act
        _userService.Login("john@test.com", "password123");

        // Assert
        _mockPasswordService.Verify(p => p.VerifyPassword("password123", "hashedPassword123"), Times.Once);
    }

    #endregion
}
