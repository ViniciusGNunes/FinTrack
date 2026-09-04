using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using FinTrack.DTO.User;
using FinTrack.Services;

[ApiController]
[Route("v1/api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly OAuthService _oauthService;
    private readonly JwtService _jwtService;
    private readonly IConfiguration _configuration;

    public UsersController(
        UserService userService, 
        OAuthService oauthService,
        JwtService jwtService, 
        IConfiguration configuration)
    {
        _userService = userService;
        _oauthService = oauthService;
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
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
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
            AppendAuthCookie(tokenString);

            return Ok(new
            {
                message = "Cadastro realizado com sucesso!",
                token = tokenString,
                user = userDto
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPost("confirm-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ConfirmEmail([FromBody] FinTrack.DTO.User.ConfirmEmailDto dto)
    {
        var success = await _userService.ConfirmEmailAsync(dto.UserId, dto.Token);
        if (!success)
        {
            return BadRequest(new { message = "Link de confirmação inválido ou expirado." });
        }

        return Ok(new { message = "E-mail verificado com sucesso! Você já pode entrar na sua conta." });
    }

    [HttpPost("resend-confirmation")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> ResendConfirmation([FromBody] FinTrack.DTO.User.ResendConfirmationDto dto)
    {
        await _userService.ResendConfirmationEmailAsync(dto.Email);
        return Ok(new { message = "Se houver uma conta pendente para este e-mail, um novo link de confirmação foi enviado." });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> ForgotPassword([FromBody] FinTrack.DTO.User.ForgotPasswordDto dto)
    {
        await _userService.ForgotPasswordAsync(dto.Email);
        return Ok(new { message = "Se o e-mail estiver cadastrado, enviamos as instruções para redefinição de senha." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ResetPassword([FromBody] FinTrack.DTO.User.ResetPasswordDto dto)
    {
        var success = await _userService.ResetPasswordAsync(dto.UserId, dto.Token, dto.NewPassword);
        if (!success)
        {
            return BadRequest(new { message = "Link de redefinição inválido, expirado ou senha não atende aos requisitos." });
        }

        return Ok(new { message = "Senha redefinida com sucesso! Você já pode entrar com sua nova senha." });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Login([FromBody] LoginDto login)
    {
        try
        {
            var userDto = await _userService.LoginAsync(login);

            if (userDto is null)
                return Unauthorized(new { message = "E-mail ou senha incorretos." });

            var userEntity = new User
            {
                Id = userDto.UserID,
                Email = userDto.Email,
                Name = userDto.Name
            };

            var tokenString = _jwtService.GenerateToken(userEntity);

            AppendAuthCookie(tokenString);

            return Ok(new 
            { 
                message = "Login realizado com sucesso!",
                token = tokenString 
            });
        }
        catch (InvalidOperationException ex)
        {
            return Unauthorized(new { message = ex.Message, isEmailUnconfirmed = true });
        }
    }

    [HttpPost("google-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> GoogleLogin([FromBody] FinTrack.DTO.User.GoogleLoginDto dto)
    {
        try
        {
            UserDto userDto;
            if (!string.IsNullOrWhiteSpace(dto.Code))
            {
                userDto = await _oauthService.ProcessOAuthLoginAsync(new OAuthLoginDto
                {
                    Provider = "google",
                    Code = dto.Code,
                    RedirectUri = dto.RedirectUri ?? string.Empty
                });
            }
            else if (!string.IsNullOrWhiteSpace(dto.IdToken))
            {
                userDto = await _userService.GoogleLoginAsync(dto.IdToken);
            }
            else
            {
                return BadRequest(new { message = "Código ou Token do Google é obrigatório." });
            }

            var userEntity = new User
            {
                Id = userDto.UserID,
                Email = userDto.Email,
                Name = userDto.Name
            };

            var tokenString = _jwtService.GenerateToken(userEntity);
            AppendAuthCookie(tokenString);

            return Ok(new
            {
                message = "Login com Google realizado com sucesso!",
                token = tokenString,
                user = userDto
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Falha ao autenticar com o Google: " + ex.Message });
        }
    }

    [HttpPost("oauth-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> OAuthLogin([FromBody] OAuthLoginDto dto)
    {
        try
        {
            var userDto = await _oauthService.ProcessOAuthLoginAsync(dto);

            var userEntity = new User
            {
                Id = userDto.UserID,
                Email = userDto.Email,
                Name = userDto.Name
            };

            var tokenString = _jwtService.GenerateToken(userEntity);
            AppendAuthCookie(tokenString);

            return Ok(new
            {
                message = $"Login com {dto.Provider} realizado com sucesso!",
                token = tokenString,
                user = userDto
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = $"Falha ao autenticar com {dto.Provider}: {ex.Message}" });
        }
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