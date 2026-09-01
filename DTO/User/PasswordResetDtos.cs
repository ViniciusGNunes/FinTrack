using System.ComponentModel.DataAnnotations;

namespace FinTrack.DTO.User;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "O e-mail é obrigatório.")]
    [EmailAddress(ErrorMessage = "E-mail inválido.")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    [Required(ErrorMessage = "O ID do usuário é obrigatório.")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "O token é obrigatório.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "A nova senha é obrigatória.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter no mínimo 8 caracteres.")]
    public string NewPassword { get; set; } = string.Empty;
}
