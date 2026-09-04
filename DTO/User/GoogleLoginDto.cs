using System.ComponentModel.DataAnnotations;

namespace FinTrack.DTO.User;

public class GoogleLoginDto
{
    public string? IdToken { get; set; }
    public string? Code { get; set; }
    public string? RedirectUri { get; set; }
}
