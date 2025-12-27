using woboapi.Models;

namespace woboapi;

public interface IUserService
{
    public List<UserModel> GetAllUsers();
    public UserModel GetUser(Guid id);
    public void CreateUser(UserModel user);
    public void UpdateUser(Guid id, UserModel user);
    public void DeleteUser(Guid id);
    public UserModel Login(string email, string password);
}
