using System.ComponentModel.DataAnnotations;

public class Category
{
    public int CategoryID { get; set; }

    [Required]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Icon { get; set; } = "default-icon";

    [MaxLength(10)]
    public string ColorHex { get; set; } = "#808080";

    public int? UserID { get; set; } 

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}