using System.ComponentModel.DataAnnotations;

namespace FinTrack.DTO.User;

public class GoogleLoginDto
{
    [Required(ErrorMessage = "O token do Google é obrigatório.")]
    public string IdToken { get; set; } = string.Empty;
}
