using System.ComponentModel.DataAnnotations;

public class CategoryCreateDto
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Icon { get; set; } = "default-icon";

    [MaxLength(10)]
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Invalid Hex color code.")]
    public string ColorHex { get; set; } = "#808080";

    /// <summary>
    /// Set to specific UserID if custom, or null if creating a system global category (admin action).
    /// </summary>
    public int? UserID { get; set; }
}

public class CategoryUpdateDto
{
    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Icon { get; set; } = "default-icon";

    [MaxLength(10)]
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Invalid Hex color code.")]
    public string ColorHex { get; set; } = "#808080";
}

public class CategoryReadDto
{
    public int CategoryID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public int? UserID { get; set; }
    public bool IsSystemDefault => UserID == null;
}