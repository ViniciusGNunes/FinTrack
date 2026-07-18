using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FinTrack.DTO; 

public class UserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserService> _logger;
    private readonly UserManager<User> _userManager;
    private readonly JwtService _jwt;

    public UserService(
        AppDbContext context,
        ILogger<UserService> logger,
        UserManager<User> userManager,
        JwtService jwt)
    {
        _context = context;
        _logger = logger;
        _userManager = userManager;
        _jwt = jwt;
    }

    public async Task<List<UserDto>> GetUsersAsync()
    {
        try
        {
            return await _context.Users
                .Select(u => new UserDto
                {
                    UserID = u.Id,
                    Name = u.Name,
                    Email = u.Email
                })
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while retrieving users.");
            throw;
        }
    }

    public async Task<UserDto?> GetUserAsync(int userId)
    {
        try
        {
            if (userId <= 0)
                throw new ArgumentException("Invalid user ID.");

            return await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => new UserDto
                {
                    UserID = u.Id,
                    Name = u.Name,
                    Email = u.Email
                })
                .FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error while retrieving user with ID {UserId}.",
                userId);

            throw;
        }
    }

    public async Task<UserDto> RegisterUserAsync(RegisterDto dto)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(dto);
            string email = dto.Email.Trim().ToLowerInvariant();

            User user = new()
            {
                Name = dto.Name.Trim(),
                Email = email,
                UserName = email
            };

            // EF Core automatically populates user.Id during this database insert tracking
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                var errorMessages = string.Join(" ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException(errorMessages);
            }

            return new UserDto
            {
                UserID = user.Id, // Already populated!
                Name = user.Name,
                Email = user.Email
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while registering user.");
            throw;
        }
    }

    public async Task<UserDto?> LoginAsync(LoginDto dto)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(dto);
            string email = dto.Email.Trim().ToLowerInvariant();

            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
                return null;

            bool isPasswordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!isPasswordValid)
                return null;


            return new UserDto
            {
                UserID = user.Id,
                Name = user.Name,
                Email = user.Email
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while logging in.");
            throw;
        }
    }

    public async Task<bool> UpdateUserAsync(UpdateUserDto dto)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(dto);

            User? existingUser = await _userManager.FindByIdAsync(dto.UserID.ToString());
            if (existingUser is null)
                return false;

            string email = dto.Email.Trim().ToLowerInvariant();

            User? userWithEmail = await _userManager.FindByEmailAsync(email);
            if (userWithEmail != null && userWithEmail.Id != dto.UserID)
                throw new InvalidOperationException("Email already exists.");

            existingUser.Name = dto.Name.Trim();
            existingUser.Email = email;
            existingUser.UserName = email;

            var result = await _userManager.UpdateAsync(existingUser);
            if (!result.Succeeded)
            {
                var errorMessages = string.Join(" ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException(errorMessages);
            }


            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error while updating user with ID {UserId}.",
                dto.UserID);

            throw;
        }
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        try
        {
            if (userId <= 0)
                throw new ArgumentException("Invalid user ID.");

            User? user = await _userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                return false;

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errorMessages = string.Join(" ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException(errorMessages);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error while deleting user with ID {UserId}.",
                userId);

            throw;
        }
    }
}

