using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("v1/api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly JwtService _jwtService; 

    public UsersController(UserService userService, JwtService jwtService)
    {
        _userService = userService;
        _jwtService = jwtService;
    }

    [HttpGet]
    [Authorize] 
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers()
    {
        var users = await _userService.GetUsersAsync();
        return Ok(users);
    }

    [HttpGet("{userID:int}")]
    [Authorize] 
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(int userID)
    {
        var user = await _userService.GetUserAsync(userID);

        if (user is null)
            return NotFound($"User with ID {userID} was not found.");

        return Ok(user);
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Register([FromBody] RegisterDto register)
    {
        try
        {
            var userDto = await _userService.RegisterUserAsync(register);

            var userEntity = new User
            {
                Id = userDto.UserID,
                Email = userDto.Email,
                Name = userDto.Name
            };

            var token = _jwtService.GenerateToken(userEntity);


            return CreatedAtAction(
                nameof(GetUser),
                new { userID = userDto.UserID },
                new { Token = token, User = userDto });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Login([FromBody] LoginDto login)
    {
        var userDto = await _userService.LoginAsync(login);

        if (userDto is null)
            return Unauthorized("Invalid email or password.");

        var userEntity = new User
        {
            Id = userDto.UserID,
            Email = userDto.Email,
            Name = userDto.Name
        };

        var token = _jwtService.GenerateToken(userEntity);

        return Ok(new
        {
            Token = token,
            User = userDto
        });
    }

    [HttpPut("{userID:int}")]
    [Authorize] 
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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
    [Authorize] 
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int userID)
    {
        bool deleted = await _userService.DeleteUserAsync(userID);

        if (!deleted)
            return NotFound($"User with ID {userID} was not found.");

        return NoContent();
    }
}