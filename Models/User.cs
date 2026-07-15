using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
public class User : IdentityUser<int>
{
    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

}