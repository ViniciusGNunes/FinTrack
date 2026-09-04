using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FinTrack.Services;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", false);

// Prevent inotify file watcher limit crash in containerized Linux environments
Environment.SetEnvironmentVariable("DOTNET_USE_POLLING_FILE_WATCHER", "true");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:3000";
    var allowedOrigins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "http://localhost:3000",
        frontendUrl.TrimEnd('/')
    }.ToArray();

    options.AddPolicy("frontend", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddControllers(options =>
{
    var policy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.Authorization.AuthorizeFilter(policy));
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityCore<User>(options =>
{
    options.SignIn.RequireConfirmedEmail = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddRoles<IdentityRole<int>>()
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var cookieName = builder.Configuration["CookieSettings:Name"] ?? "X-Access-Token";
                if (context.Request.Cookies.TryGetValue(cookieName, out var cookieToken))
                {
                    context.Token = cookieToken;
                }
                return Task.CompletedTask;
            }
        };

    });

builder.Services.AddAuthorization();

builder.Services.AddHttpClient();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<OAuthService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<IMarketDataService, MarketDataService>();
builder.Services.AddScoped<InvestmentService>();
builder.Services.AddScoped<DebtService>();
builder.Services.AddScoped<ReceivableService>();
builder.Services.AddScoped<GoalService>();

var app = builder.Build();

app.UseCors("frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();


app.Run();