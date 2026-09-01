using System.ComponentModel.DataAnnotations;

namespace FinTrack.DTO.User;

public class ConfirmEmailDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public string Token { get; set; } = string.Empty;
}

public class ResendConfirmationDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}
