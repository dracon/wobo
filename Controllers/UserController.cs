using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using woboapi.Models;

namespace woboapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: api/User
        [HttpGet]
        public ActionResult<List<UserModel>> GetAllUsers()
        {
            var users = _userService.GetAllUsers();
            return Ok(users);
        }

        // GET api/User/{id}
        [HttpGet("{id}")]
        public ActionResult<UserModel> GetUser(Guid id)
        {
            var user = _userService.GetUser(id);
            return Ok(user);
        }

        // POST api/User
        [HttpPost]
        [AllowAnonymous]
        public ActionResult CreateUser([FromBody] UserModel user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _userService.CreateUser(user);
            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }

        // PUT api/User/{id}
        [HttpPut("{id}")]
        public ActionResult UpdateUser(Guid id, [FromBody] UserModel user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _userService.UpdateUser(id, user);
            return NoContent();
        }

        // DELETE api/User/{id}
        [HttpDelete("{id}")]
        public ActionResult DeleteUser(Guid id)
        {
            _userService.DeleteUser(id);
            return NoContent();
        }
    }
}
