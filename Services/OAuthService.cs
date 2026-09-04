using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinTrack.DTO;
using FinTrack.DTO.User;
using Microsoft.AspNetCore.Identity;

namespace FinTrack.Services;

public class OAuthService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OAuthService> _logger;
    private readonly UserManager<User> _userManager;

    public OAuthService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<OAuthService> logger,
        UserManager<User> userManager)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _userManager = userManager;
    }

    public async Task<UserDto> ProcessOAuthLoginAsync(OAuthLoginDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        var provider = dto.Provider.Trim().ToLowerInvariant();

        OAuthUserInfo userInfo = provider switch
        {
            "google" => await HandleGoogleLoginAsync(dto),
            "github" => await HandleGitHubLoginAsync(dto),
            "discord" => await HandleDiscordLoginAsync(dto),
            "twitter" or "x" => await HandleTwitterLoginAsync(dto),
            _ => throw new InvalidOperationException($"Provedor OAuth '{dto.Provider}' não suportado.")
        };

        return await FindOrCreateUserAsync(userInfo);
    }

    private async Task<UserDto> FindOrCreateUserAsync(OAuthUserInfo userInfo)
    {
        if (string.IsNullOrWhiteSpace(userInfo.Email))
        {
            throw new InvalidOperationException("Não foi possível obter um e-mail válido da sua conta no provedor.");
        }

        string email = userInfo.Email.Trim().ToLowerInvariant();
        var user = await _userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new User
            {
                Name = !string.IsNullOrWhiteSpace(userInfo.Name) ? userInfo.Name.Trim() : "Usuário " + userInfo.Provider,
                Email = email,
                UserName = email,
                EmailConfirmed = true
            };

            var randomPassword = Guid.NewGuid().ToString("N") + "!1Aa";
            var result = await _userManager.CreateAsync(user, randomPassword);
            if (!result.Succeeded)
            {
                var errorMessages = string.Join(" ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Erro ao registrar usuário OAuth: {errorMessages}");
            }
        }
        else if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
        }

        return new UserDto
        {
            UserID = user.Id,
            Name = user.Name,
            Email = user.Email
        };
    }

    #region Google Provider
    private async Task<OAuthUserInfo> HandleGoogleLoginAsync(OAuthLoginDto dto)
    {
        var clientId = _configuration["Google:ClientId"]
                       ?? _configuration["Authentication:Google:ClientId"]
                       ?? throw new InvalidOperationException("Google ClientId não configurado.");
        var clientSecret = _configuration["Google:ClientSecret"]
                           ?? _configuration["Authentication:Google:ClientSecret"]
                           ?? throw new InvalidOperationException("Google ClientSecret não configurado.");

        var client = _httpClientFactory.CreateClient();
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = dto.Code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = !string.IsNullOrWhiteSpace(dto.RedirectUri) ? dto.RedirectUri : "postmessage"
            })
        };

        var tokenResponse = await client.SendAsync(tokenRequest);
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Google token exchange failed: {Body}", tokenBody);
            throw new InvalidOperationException("Falha ao autenticar com o Google. Código inválido ou expirado.");
        }

        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenBody);
        var idToken = tokenData.GetProperty("id_token").GetString()!;

        var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken, new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { clientId }
        });

        return new OAuthUserInfo
        {
            Provider = "Google",
            Name = !string.IsNullOrWhiteSpace(payload.Name) ? payload.Name : payload.GivenName ?? "Usuário Google",
            Email = payload.Email
        };
    }
    #endregion

    #region GitHub Provider

    private async Task<OAuthUserInfo> HandleGitHubLoginAsync(OAuthLoginDto dto)
    {
        var clientId = _configuration["Authentication:GitHub:ClientId"]
                       ?? _configuration["GitHub:ClientId"]
                       ?? throw new InvalidOperationException("GitHub ClientId não configurado.");
        var clientSecret = _configuration["Authentication:GitHub:ClientSecret"]
                           ?? _configuration["GitHub:ClientSecret"]
                           ?? throw new InvalidOperationException("GitHub ClientSecret não configurado.");

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("FinTrack-App", "1.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // 1. Troca o código pelo access_token
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://github.com/login/oauth/access_token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = dto.Code,
                ["redirect_uri"] = dto.RedirectUri
            })
        };

        var tokenResponse = await client.SendAsync(tokenRequest);
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError("GitHub token exchange failed: {Body}", tokenBody);
            throw new InvalidOperationException("Falha ao autenticar com o GitHub. Código inválido ou expirado.");
        }

        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenBody);
        if (!tokenData.TryGetProperty("access_token", out var accessTokenElem))
        {
            var err = tokenData.TryGetProperty("error_description", out var desc) ? desc.GetString() : "Token não retornado.";
            throw new InvalidOperationException($"GitHub OAuth erro: {err}");
        }

        var accessToken = accessTokenElem.GetString()!;

        // 2. Busca o perfil do usuário
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var profileResponse = await client.GetAsync("https://api.github.com/user");
        var profileBody = await profileResponse.Content.ReadAsStringAsync();

        if (!profileResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Falha ao recuperar perfil do GitHub.");
        }

        var profile = JsonSerializer.Deserialize<JsonElement>(profileBody);
        var name = profile.TryGetProperty("name", out var n) && n.ValueKind != JsonValueKind.Null ? n.GetString() : null;
        var login = profile.TryGetProperty("login", out var l) ? l.GetString() : "github_user";
        var email = profile.TryGetProperty("email", out var e) && e.ValueKind != JsonValueKind.Null ? e.GetString() : null;

        // Se e-mail for privado, busca os e-mails do usuário
        if (string.IsNullOrWhiteSpace(email))
        {
            var emailsResponse = await client.GetAsync("https://api.github.com/user/emails");
            if (emailsResponse.IsSuccessStatusCode)
            {
                var emailsBody = await emailsResponse.Content.ReadAsStringAsync();
                var emails = JsonSerializer.Deserialize<JsonElement>(emailsBody);
                if (emails.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in emails.EnumerateArray())
                    {
                        var isPrimary = item.TryGetProperty("primary", out var p) && p.GetBoolean();
                        var isVerified = item.TryGetProperty("verified", out var v) && v.GetBoolean();
                        if (isPrimary && isVerified && item.TryGetProperty("email", out var mailElem))
                        {
                            email = mailElem.GetString();
                            break;
                        }
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            email = $"{login}@users.noreply.github.com";
        }

        return new OAuthUserInfo
        {
            Provider = "GitHub",
            Name = name ?? login,
            Email = email
        };
    }

    #endregion

    #region Discord Provider

    private async Task<OAuthUserInfo> HandleDiscordLoginAsync(OAuthLoginDto dto)
    {
        var clientId = _configuration["Authentication:Discord:ClientId"]
                       ?? _configuration["Discord:ClientId"]
                       ?? throw new InvalidOperationException("Discord ClientId não configurado.");
        var clientSecret = _configuration["Authentication:Discord:ClientSecret"]
                           ?? _configuration["Discord:ClientSecret"]
                           ?? throw new InvalidOperationException("Discord ClientSecret não configurado.");

        var client = _httpClientFactory.CreateClient();

        // 1. Troca o código pelo token
        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = dto.Code,
                ["redirect_uri"] = dto.RedirectUri
            })
        };

        var tokenResponse = await client.SendAsync(tokenRequest);
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Discord token exchange failed: {Body}", tokenBody);
            throw new InvalidOperationException("Falha ao autenticar com o Discord. Código inválido ou expirado.");
        }

        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenBody);
        var accessToken = tokenData.GetProperty("access_token").GetString()!;

        // 2. Busca informações do usuário
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var profileResponse = await client.GetAsync("https://discord.com/api/users/@me");
        var profileBody = await profileResponse.Content.ReadAsStringAsync();

        if (!profileResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Falha ao recuperar perfil do Discord.");
        }

        var profile = JsonSerializer.Deserialize<JsonElement>(profileBody);
        var username = profile.GetProperty("username").GetString()!;
        var globalName = profile.TryGetProperty("global_name", out var gn) && gn.ValueKind != JsonValueKind.Null ? gn.GetString() : null;
        var email = profile.TryGetProperty("email", out var e) && e.ValueKind != JsonValueKind.Null ? e.GetString() : null;

        if (string.IsNullOrWhiteSpace(email))
        {
            var id = profile.GetProperty("id").GetString();
            email = $"{id}@discord.fintrack.internal";
        }

        return new OAuthUserInfo
        {
            Provider = "Discord",
            Name = globalName ?? username,
            Email = email
        };
    }

    #endregion

    #region Twitter / X Provider

    private async Task<OAuthUserInfo> HandleTwitterLoginAsync(OAuthLoginDto dto)
    {
        var clientId = _configuration["Authentication:Twitter:ClientId"]
                       ?? _configuration["Twitter:ClientId"]
                       ?? throw new InvalidOperationException("Twitter ClientId não configurado.");
        var clientSecret = _configuration["Authentication:Twitter:ClientSecret"]
                           ?? _configuration["Twitter:ClientSecret"];

        var client = _httpClientFactory.CreateClient();

        var postParams = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = dto.Code,
            ["redirect_uri"] = dto.RedirectUri,
            ["client_id"] = clientId,
            ["code_verifier"] = dto.CodeVerifier ?? "challenge"
        };

        var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.twitter.com/2/oauth2/token")
        {
            Content = new FormUrlEncodedContent(postParams)
        };

        if (!string.IsNullOrWhiteSpace(clientSecret))
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            tokenRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        }

        var tokenResponse = await client.SendAsync(tokenRequest);
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogError("Twitter token exchange failed: {Body}", tokenBody);
            throw new InvalidOperationException("Falha ao autenticar com o X/Twitter. Código inválido ou expirado.");
        }

        var tokenData = JsonSerializer.Deserialize<JsonElement>(tokenBody);
        var accessToken = tokenData.GetProperty("access_token").GetString()!;

        // 2. Busca informações do usuário
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var profileResponse = await client.GetAsync("https://api.twitter.com/2/users/me");
        var profileBody = await profileResponse.Content.ReadAsStringAsync();

        if (!profileResponse.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("Falha ao recuperar perfil do X/Twitter.");
        }

        var profileDoc = JsonSerializer.Deserialize<JsonElement>(profileBody);
        var data = profileDoc.GetProperty("data");
        var name = data.GetProperty("name").GetString()!;
        var username = data.GetProperty("username").GetString()!;

        // X OAuth 2.0 padrão nem sempre devolve e-mail sem aprovação elevada; usamos e-mail sintético determinístico
        var email = $"{username.ToLowerInvariant()}@twitter.fintrack.internal";

        return new OAuthUserInfo
        {
            Provider = "Twitter",
            Name = name,
            Email = email
        };
    }

    #endregion

    private record OAuthUserInfo
    {
        public required string Provider { get; init; }
        public required string Name { get; init; }
        public required string Email { get; init; }
    }
}
