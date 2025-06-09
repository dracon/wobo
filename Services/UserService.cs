using woboapi.Exceptions;
using woboapi.Models;

namespace woboapi;

public class UserService : IUserService
{
    private readonly ILogger<UserService> _logger;

    public UserService(ILogger<UserService> logger)
    {
        _logger = logger;
    }

    public List<UserModel> GetAllUsers()
    {
        _logger.LogInformation("Getting all users");
        throw new NotImplementedException();
    }

    public UserModel GetUser(Guid id)
    {
        _logger.LogInformation("Getting user with id: {id}", id);
        throw new UserNotFoundException("User with id " + id + " not found.");
    }

    public void CreateUser(UserModel user)
    {
        throw new NotImplementedException();
    }
}
