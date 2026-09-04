using System.ComponentModel.DataAnnotations;

namespace FinTrack.DTO.User;

public class OAuthLoginDto
{
    [Required(ErrorMessage = "O provedor é obrigatório.")]
    public string Provider { get; set; } = string.Empty; // "github", "discord", "twitter"

    [Required(ErrorMessage = "O código de autorização é obrigatório.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "O redirect_uri é obrigatório.")]
    public string RedirectUri { get; set; } = string.Empty;

    public string? CodeVerifier { get; set; } // Necessário para Twitter PKCE
}
