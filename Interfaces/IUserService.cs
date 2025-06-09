using woboapi.Models;

namespace woboapi;

public interface IUserService
{
    public List<UserModel> GetAllUsers();
    public UserModel GetUser(Guid id);
    
    public void CreateUser(UserModel user);
}
