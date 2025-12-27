using Microsoft.AspNetCore.Mvc;
using woboapi.Models;
using woboapi.Services;

namespace woboapi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IConfiguration _configuration;

    public AuthController(IUserService userService, IJwtTokenService jwtTokenService, IConfiguration configuration)
    {
        _userService = userService;
        _jwtTokenService = jwtTokenService;
        _configuration = configuration;
    }

    // POST api/Auth/login
    [HttpPost("login")]
    public ActionResult<LoginResponse> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = _userService.Login(request.Email, request.Password);
        var token = _jwtTokenService.GenerateToken(user);

        var expiryInMinutes = Convert.ToDouble(_configuration["Jwt:ExpiryInMinutes"]);
        var response = new LoginResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryInMinutes),
            User = user
        };

        return Ok(response);
    }
}
