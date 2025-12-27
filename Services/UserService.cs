using Microsoft.EntityFrameworkCore;
using woboapi.Data;
using woboapi.Exceptions;
using woboapi.Models;
using woboapi.Services;

namespace woboapi;

public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IPasswordService _passwordService;

    public UserService(ILogger<UserService> logger, ApplicationDbContext context, IPasswordService passwordService)
    {
        _logger = logger;
        _context = context;
        _passwordService = passwordService;
    }

    public List<UserModel> GetAllUsers()
    {
        _logger.LogInformation("Getting all users");
        var users = _context.Users.ToList();

        // Clear passwords before returning
        foreach (var user in users)
        {
            user.Password = null;
        }

        return users;
    }

    public UserModel GetUser(Guid id)
    {
        _logger.LogInformation("Getting user with id: {id}", id);
        var user = _context.Users.Find(id);

        if (user == null)
        {
            throw new UserNotFoundException("User with id " + id + " not found.");
        }

        // Clear password before returning
        user.Password = null;

        return user;
    }

    public void CreateUser(UserModel user)
    {
        _logger.LogInformation("Creating user with email: {email}", user.Email);

        // Check for duplicate email
        if (_context.Users.Any(u => u.Email == user.Email))
        {
            throw new DuplicateEmailException("A user with email " + user.Email + " already exists.");
        }

        user.Id = Guid.NewGuid();
        user.Password = _passwordService.HashPassword(user.Password!);
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        _context.Users.Add(user);
        _context.SaveChanges();

        // Clear password before returning
        user.Password = null;
    }

    public void UpdateUser(Guid id, UserModel user)
    {
        _logger.LogInformation("Updating user with id: {id}", id);

        var existingUser = _context.Users.Find(id);

        if (existingUser == null)
        {
            throw new UserNotFoundException("User with id " + id + " not found.");
        }

        // Check for duplicate email (excluding current user)
        if (_context.Users.Any(u => u.Email == user.Email && u.Id != id))
        {
            throw new DuplicateEmailException("A user with email " + user.Email + " already exists.");
        }

        existingUser.Name = user.Name;
        existingUser.Email = user.Email;
        existingUser.Password = _passwordService.HashPassword(user.Password!);
        existingUser.Gender = user.Gender;
        existingUser.UpdatedAt = DateTime.UtcNow;

        _context.SaveChanges();
    }

    public void DeleteUser(Guid id)
    {
        _logger.LogInformation("Deleting user with id: {id}", id);

        var user = _context.Users.Find(id);

        if (user == null)
        {
            throw new UserNotFoundException("User with id " + id + " not found.");
        }

        _context.Users.Remove(user);
        _context.SaveChanges();
    }

    public UserModel Login(string email, string password)
    {
        _logger.LogInformation("Login attempt for email: {email}", email);

        var user = _context.Users.FirstOrDefault(u => u.Email == email);

        if (user == null)
        {
            throw new InvalidCredentialsException("Invalid email or password.");
        }

        if (!_passwordService.VerifyPassword(password, user.Password!))
        {
            throw new InvalidCredentialsException("Invalid email or password.");
        }

        // Clear password before returning
        user.Password = null;

        return user;
    }
}
