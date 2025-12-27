namespace woboapi.Models;

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public UserModel User { get; set; } = null!;
}
