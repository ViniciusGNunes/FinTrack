using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("v1/api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly JwtService _jwtService;
    private readonly IConfiguration _configuration;

    public UsersController(UserService userService, JwtService jwtService, IConfiguration configuration)
    {
        _userService = userService;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    private int GetAuthenticatedUserId()
    {
        var userIdClaim = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        throw new UnauthorizedAccessException("Usuário não autenticado ou identificador inválido.");
    }

    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var authUserId = GetAuthenticatedUserId();
        var user = await _userService.GetUserAsync(authUserId);
        if (user is null)
            return NotFound("Usuário não encontrado.");

        return Ok(user);
    }

    [HttpGet("{userID:int}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(int userID)
    {
        var authUserId = GetAuthenticatedUserId();
        // Ensure user can only read their own profile
        var user = await _userService.GetUserAsync(authUserId);

        if (user is null)
            return NotFound($"User with ID {authUserId} was not found.");

        return Ok(user);
    }

    [HttpPost("register")]
    [AllowAnonymous]
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

            var tokenString = _jwtService.GenerateToken(userEntity);

            // Using the helper method now
            AppendAuthCookie(tokenString);

            return Ok(new { message = "Registered and logged in successfully!" });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
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

        var tokenString = _jwtService.GenerateToken(userEntity);

        // Using the helper method now
        AppendAuthCookie(tokenString);

        return Ok(new { message = "Logged in successfully!" });
    }

    // New Endpoint: Added a Logout handler since you are managing cookies now!
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        var cookieName = _configuration["CookieSettings:Name"] ?? "X-Access-Token";
        Response.Cookies.Delete(cookieName);
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpPut("{userID:int?}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Update(int? userID, [FromBody] UpdateUserDto update)
    {
        var authUserId = GetAuthenticatedUserId();
        update.UserID = authUserId;

        try
        {
            bool updated = await _userService.UpdateUserAsync(update);

            if (!updated)
                return NotFound($"User with ID {authUserId} was not found.");

            var user = await _userService.GetUserAsync(authUserId);
            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }

    [HttpDelete("{userID:int?}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int? userID)
    {
        var authUserId = GetAuthenticatedUserId();
        bool deleted = await _userService.DeleteUserAsync(authUserId);

        if (!deleted)
            return NotFound($"User with ID {authUserId} was not found.");

        return NoContent();
    }

    private void AppendAuthCookie(string tokenString)
    {
        var cookieName = _configuration["CookieSettings:Name"] ?? "X-Access-Token";
        var minutes = int.Parse(_configuration["CookieSettings:ExpireTimeInMinutes"] ?? "60");
        var isSecure = bool.Parse(_configuration["CookieSettings:Secure"] ?? "true");
        var sameSiteStr = _configuration["CookieSettings:SameSite"] ?? "Strict";

        Enum.TryParse(sameSiteStr, out SameSiteMode sameSiteMode);

        var cookieOptions = new CookieOptions
        {
            HttpOnly = false,
            Secure = isSecure,
            SameSite = sameSiteMode,
            Expires = DateTime.UtcNow.AddMinutes(minutes),
        };

        Response.Cookies.Append(cookieName, tokenString, cookieOptions);
    }
}