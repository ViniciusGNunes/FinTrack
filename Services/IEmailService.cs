public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string userName, string verificationUrl);
    Task SendPasswordResetEmailAsync(string toEmail, string userName, string resetUrl);
}
