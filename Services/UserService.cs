using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class UserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserService> _logger;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(
        AppDbContext context,
        ILogger<UserService> logger,
        IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _logger = logger;
        _passwordHasher = passwordHasher;
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

            bool exists = await _context.Users
                .AnyAsync(u => u.Email == email);

            if (exists)
                throw new InvalidOperationException("Email already exists.");

            User user = new()
            {
                Name = dto.Name.Trim(),
                Email = email,
                PasswordHash = string.Empty
            };

            user.PasswordHash =
                _passwordHasher.HashPassword(user, dto.Password);

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            return new UserDto
            {
                UserID = user.Id,
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

            User? user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user is null)
                return null;

            PasswordVerificationResult result =
                _passwordHasher.VerifyHashedPassword(
                    user,
                    user.PasswordHash,
                    dto.Password);

            if (result == PasswordVerificationResult.Failed)
                return null;

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash =
                    _passwordHasher.HashPassword(user, dto.Password);

                await _context.SaveChangesAsync();
            }

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

            User? existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == dto.UserID);

            if (existingUser is null)
                return false;

            string email = dto.Email.Trim().ToLowerInvariant();

            bool emailExists = await _context.Users.AnyAsync(u =>
                u.Email == email &&
                u.Id != dto.UserID);

            if (emailExists)
                throw new InvalidOperationException("Email already exists.");

            existingUser.Name = dto.Name.Trim();
            existingUser.Email = email;

            await _context.SaveChangesAsync();

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

            User? user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null)
                return false;

            _context.Users.Remove(user);

            await _context.SaveChangesAsync();

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