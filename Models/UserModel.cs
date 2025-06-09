namespace woboapi.Models;

public class UserModel
{
    public UserModel(string name, string email, string password)
    {
        Name = name;
        Email = email;
        Password = password;
    }

    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public Gender Gender { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}