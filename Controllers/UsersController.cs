using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("v1/api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _userService.GetUsersAsync();
        return Ok(users);
    }

    [HttpGet("{userID:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(int userID)
    {
        var user = await _userService.GetUserAsync(userID);

        if (user is null)
            return NotFound($"User with ID {userID} was not found.");

        return Ok(user);
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterDto register)
    {
        try
        {
            var createdUser = await _userService.RegisterUserAsync(register);

            return CreatedAtAction(
                nameof(GetUser),
                new { userID = createdUser.UserID },
                createdUser);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginDto login)
    {
        var user = await _userService.LoginAsync(login);

        if (user is null)
            return Unauthorized("Invalid email or password.");

        return Ok(user);
    }

    [HttpPut("{userID:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Update(
        int userID,
        [FromBody] UpdateUserDto update)
    {
        if (userID != update.UserID)
            return BadRequest("Route ID does not match request body.");

        try
        {
            bool updated = await _userService.UpdateUserAsync(update);

            if (!updated)
                return NotFound($"User with ID {userID} was not found.");

            var user = await _userService.GetUserAsync(userID);

            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{userID:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int userID)
    {
        bool deleted = await _userService.DeleteUserAsync(userID);

        if (!deleted)
            return NotFound($"User with ID {userID} was not found.");

        return NoContent();
    }
}