using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration, HttpClient httpClient)
    {
        _logger = logger;
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string userName, string verificationUrl)
    {
        _logger.LogInformation("\n========================================================");
        _logger.LogInformation("📧 [CONFIRMAÇÃO DE E-MAIL - FINTRACK]");
        _logger.LogInformation("👤 Destinatário: {UserName} ({Email})", userName, toEmail);
        _logger.LogInformation("🔗 Link de Verificação: {Link}", verificationUrl);
        _logger.LogInformation("========================================================\n");

        var html = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #090d16; color: #f8fafc; margin: 0; padding: 40px 20px; }}
    .card {{ max-width: 500px; margin: 0 auto; background: #111827; border: 1px solid #1e293b; border-radius: 12px; padding: 32px; text-align: center; }}
    .badge {{ display: inline-block; width: 44px; height: 44px; background: #10b981; color: #ffffff; border-radius: 8px; font-size: 22px; font-weight: 800; line-height: 44px; margin-bottom: 20px; }}
    h1 {{ font-size: 22px; font-weight: 700; color: #f8fafc; margin-bottom: 12px; }}
    p {{ font-size: 15px; color: #94a3b8; line-height: 1.6; margin-bottom: 28px; }}
    .btn {{ display: inline-block; background: #10b981; color: #ffffff; text-decoration: none; padding: 14px 28px; border-radius: 6px; font-weight: 600; font-size: 15px; }}
    .footer {{ font-size: 12px; color: #64748b; margin-top: 24px; }}
  </style>
</head>
<body>
  <div class='card'>
    <div class='badge'>F</div>
    <h1>Confirme seu E-mail</h1>
    <p>Olá <strong>{userName}</strong>,<br>Obrigado por se cadastrar no <strong>FinTrack</strong>! Para ativar sua conta e acessar seus dados com total segurança, clique no botão abaixo:</p>
    <a href='{verificationUrl}' class='btn' target='_blank'>Confirmar Meu E-mail</a>
    <p class='footer'>Se você não solicitou este cadastro, pode ignorar este e-mail.</p>
  </div>
</body>
</html>";

        await SendViaResendAsync(toEmail, "Confirme seu e-mail no FinTrack", html);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetUrl)
    {
        _logger.LogInformation("\n========================================================");
        _logger.LogInformation("🔐 [RECUPERAÇÃO DE SENHA - FINTRACK]");
        _logger.LogInformation("👤 Destinatário: {UserName} ({Email})", userName, toEmail);
        _logger.LogInformation("🔗 Link de Redefinição: {Link}", resetUrl);
        _logger.LogInformation("========================================================\n");

        var html = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; background-color: #090d16; color: #f8fafc; margin: 0; padding: 40px 20px; }}
    .card {{ max-width: 500px; margin: 0 auto; background: #111827; border: 1px solid #1e293b; border-radius: 12px; padding: 32px; text-align: center; }}
    .badge {{ display: inline-block; width: 44px; height: 44px; background: #10b981; color: #ffffff; border-radius: 8px; font-size: 22px; font-weight: 800; line-height: 44px; margin-bottom: 20px; }}
    h1 {{ font-size: 22px; font-weight: 700; color: #f8fafc; margin-bottom: 12px; }}
    p {{ font-size: 15px; color: #94a3b8; line-height: 1.6; margin-bottom: 28px; }}
    .btn {{ display: inline-block; background: #10b981; color: #ffffff; text-decoration: none; padding: 14px 28px; border-radius: 6px; font-weight: 600; font-size: 15px; }}
    .footer {{ font-size: 12px; color: #64748b; margin-top: 24px; }}
  </style>
</head>
<body>
  <div class='card'>
    <div class='badge'>F</div>
    <h1>Redefinição de Senha</h1>
    <p>Olá <strong>{userName}</strong>,<br>Recebemos uma solicitação para redefinir a senha da sua conta no <strong>FinTrack</strong>. Clique no botão abaixo para cadastrar sua nova senha:</p>
    <a href='{resetUrl}' class='btn' target='_blank'>Redefinir Minha Senha</a>
    <p class='footer'>Este link expira em 2 horas. Se você não fez essa solicitação, desconsidere este e-mail.</p>
  </div>
</body>
</html>";

        await SendViaResendAsync(toEmail, "Redefinição de senha no FinTrack", html);
    }

    private async Task SendViaResendAsync(string toEmail, string subject, string htmlContent)
    {
        try
        {
            var apiKey = _configuration["Resend:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                _logger.LogWarning("Resend:ApiKey não configurada no appsettings.json.");
                return;
            }

            var fromEmail = _configuration["Resend:FromEmail"] ?? "FinTrack <onboarding@resend.dev>";

            var payload = new
            {
                from = fromEmail,
                to = new[] { toEmail },
                subject = subject,
                html = htmlContent
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("✅ E-mail enviado com sucesso via Resend para {Email}!", toEmail);
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("⚠️ Resend retornou status {StatusCode}: {ErrorBody}", response.StatusCode, errorBody);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar e-mail via Resend para {Email}", toEmail);
        }
    }
}
