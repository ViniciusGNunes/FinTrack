using System.ComponentModel.DataAnnotations;

public class UpdateUserDto
{
    [Required]
    public int UserID { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public required string Name { get; set; }

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public required string Email { get; set; }
}