using CleanArchApiGenerator.API.Config;

namespace CleanArchApiGenerator.API.Services
{
    /// <summary>
    /// Pure content factory: generates C# source file contents as strings.
    /// No file I/O or CLI calls happen here.
    /// </summary>
    public class FileContentGenerator
    {
        private readonly GeneratorOptions _opts;

        public FileContentGenerator(GeneratorOptions opts)
        {
            _opts = opts;
        }

        public string ProgramCs(string projectName) => $@"
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.OpenApi.Models;
using {projectName}.Infrastructure;
using {projectName}.API.Middleware;
using {projectName}.Application.Validators;
using {projectName}.Application.Common.Results;
using {projectName}.Infrastructure.Persistence;
using {projectName}.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{{
    c.SwaggerDoc(""v1"", new OpenApiInfo
    {{
        Title = ""{projectName} API"",
        Version = ""v1"",
        Description = ""Enterprise-ready API Boilerplate""
    }});

    c.AddSecurityDefinition(""Bearer"", new OpenApiSecurityScheme
    {{
        Name = ""Authorization"",
        Type = SecuritySchemeType.Http,
        Scheme = ""bearer"",
        BearerFormat = ""JWT"",
        In = ParameterLocation.Header,
        Description = ""Paste your JWT token here (without 'Bearer ').""
    }});

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {{
        {{
            new OpenApiSecurityScheme
            {{
                Reference = new OpenApiReference
                {{
                    Type = ReferenceType.SecurityScheme,
                    Id = ""Bearer""
                }}
            }},
            Array.Empty<string>()
        }}
    }});
}});

builder.Services.AddInfrastructure(builder.Configuration);

var jwtSettings = builder.Configuration.GetSection(""Jwt"");

builder.Services.AddAuthentication(options =>
{{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}})
.AddJwtBearer(options =>
{{
    options.TokenValidationParameters = new TokenValidationParameters
    {{
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings[""Issuer""],
        ValidAudience = jwtSettings[""Audience""],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSettings[""Key""]!))
    }};
}});

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{{
    options.InvalidModelStateResponseFactory = context =>
    {{
        var errors = context.ModelState
            .Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

        return new BadRequestObjectResult(
            Result.Fail(errors, ""Validation Failed""));
    }};
}});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{{
    try
    {{
        await DbSeeder.SeedAsync(scope.ServiceProvider);
    }}
    catch (Exception ex)
    {{
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, ""Database seeding failed."");
    }}
}}

app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{{
    app.UseSwagger();
    app.UseSwaggerUI();
}}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
";

        public string BaseApiController(string projectName) => $@"
using Microsoft.AspNetCore.Mvc;

namespace {projectName}.API.Controllers;

[ApiController]
[Route(""api/[controller]"")]
public abstract class BaseApiController : ControllerBase
{{
}}
";

        public string AuthController(string projectName) => $@"
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using {projectName}.Infrastructure.Identity;
using {projectName}.Domain.Entities;
using {projectName}.Application.DTOs.Auth;
using {projectName}.Application.Common.Results;

namespace {projectName}.API.Controllers;

public class AuthController : BaseApiController
{{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenService _tokenService;

    public AuthController(UserManager<ApplicationUser> userManager, JwtTokenService tokenService)
    {{
        _userManager = userManager;
        _tokenService = tokenService;
    }}

    [HttpPost(""register"")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {{
        var user = new ApplicationUser
        {{
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        }};

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {{
            var errors = result.Errors.Select(e => e.Description).ToList();
            return BadRequest(Result.Fail(errors, ""Registration Failed""));
        }}

        return Ok(Result.Ok(""User registered successfully.""));
    }}

    [HttpPost(""login"")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {{
        var user = await _userManager.FindByEmailAsync(request.Email);

        if (user == null || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(Result.Fail(new List<string> {{ ""Invalid credentials."" }}, ""Authentication Failed""));

        var token = await _tokenService.GenerateTokenAsync(user);
        return Ok(Result<string>.Ok(token, ""Login successful.""));
    }}
}}
";

        public string AppSettings(string projectName) => $@"{{
  ""ConnectionStrings"": {{
    ""DefaultConnection"": ""Server=localhost\\SQLEXPRESS;Database={projectName}Db;Trusted_Connection=True;TrustServerCertificate=True;""
  }},
  ""Jwt"": {{
    ""Key"": ""{_opts.DefaultJwtKey}"",
    ""Issuer"": ""{_opts.DefaultJwtIssuer}"",
    ""Audience"": ""{_opts.DefaultJwtAudience}"",
    ""ExpiryMinutes"": {_opts.DefaultJwtExpiryMinutes}
  }},
  ""Logging"": {{
    ""LogLevel"": {{
      ""Default"": ""Information"",
      ""Microsoft.AspNetCore"": ""Warning""
    }}
  }},
  ""AllowedHosts"": ""*""
}}
";

        public string DbContext(string projectName) => $@"
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using {projectName}.Domain.Entities;

namespace {projectName}.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {{
    }}

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {{
        base.OnModelCreating(modelBuilder);
    }}
}}
";

        public string DependencyInjection(string projectName) => $@"
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using {projectName}.Domain.Entities;
using {projectName}.Infrastructure.Identity;
using {projectName}.Infrastructure.Persistence;
using {projectName}.Infrastructure.Identity;

namespace {projectName}.Infrastructure;

public static class DependencyInjection
{{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {{
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString(""DefaultConnection"")));

        services.AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<JwtTokenService>();

        return services;
    }}
}}
";

        public string ApplicationUser(string projectName) => $@"
using Microsoft.AspNetCore.Identity;

namespace {projectName}.Domain.Entities;

public class ApplicationUser : IdentityUser
{{
    public string? FirstName {{ get; set; }}
    public string? LastName {{ get; set; }}
}}
";

        /// <summary>
        /// Fixed: GenerateTokenAsync is now truly async — no more .Wait() blocking.
        /// </summary>
        public string JwtTokenService(string projectName) => $@"
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using {projectName}.Domain.Entities;

namespace {projectName}.Infrastructure.Identity;

public class JwtTokenService
{{
    private readonly IConfiguration _configuration;
    private readonly UserManager<ApplicationUser> _userManager;

    public JwtTokenService(IConfiguration configuration, UserManager<ApplicationUser> userManager)
    {{
        _configuration = configuration;
        _userManager = userManager;
    }}

    public async Task<string> GenerateTokenAsync(ApplicationUser user)
    {{
        var jwtSettings = _configuration.GetSection(""Jwt"");
        var roles = await _userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {{
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        }};

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings[""Key""]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(double.Parse(jwtSettings[""ExpiryMinutes""]!));

        var token = new JwtSecurityToken(
            issuer: jwtSettings[""Issuer""],
            audience: jwtSettings[""Audience""],
            claims: claims,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }}
}}
";

        public string ExceptionMiddleware(string projectName) => $@"
using System.Net;
using {projectName}.Application.Common.Results;

namespace {projectName}.API.Middleware;

public class ExceptionMiddleware
{{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {{
        _next = next;
        _logger = logger;
    }}

    public async Task InvokeAsync(HttpContext context)
    {{
        try
        {{
            await _next(context);
        }}
        catch (Exception ex)
        {{
            _logger.LogError(ex, ""Unhandled exception."");
            await HandleExceptionAsync(context, ex);
        }}
    }}

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {{
        context.Response.ContentType = ""application/json"";

        (context.Response.StatusCode, var response) = exception switch
        {{
            UnauthorizedAccessException =>
                ((int)HttpStatusCode.Unauthorized,
                 Result.Fail(new List<string> {{ ""Unauthorized access."" }}, ""Access Denied"")),

            ArgumentException argEx =>
                ((int)HttpStatusCode.BadRequest,
                 Result.Fail(new List<string> {{ argEx.Message }}, ""Bad Request"")),

            _ =>
                ((int)HttpStatusCode.InternalServerError,
                 Result.Fail(new List<string> {{ ""An unexpected error occurred."" }}, ""Internal Server Error""))
        }};

        await context.Response.WriteAsJsonAsync(response);
    }}
}}
";

        public string MiddlewareExtensions(string projectName) => $@"
namespace {projectName}.API.Middleware;

public static class MiddlewareExtensions
{{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        => app.UseMiddleware<ExceptionMiddleware>();
}}
";

        public string ResultClass(string projectName) => $@"
namespace {projectName}.Application.Common.Results;

public class Result<T>
{{
    public bool Success {{ get; private set; }}
    public string? Message {{ get; private set; }}
    public T? Data {{ get; private set; }}
    public List<string>? Errors {{ get; private set; }}

    private Result() {{ }}

    public static Result<T> Ok(T data, string? message = null) =>
        new() {{ Success = true, Data = data, Message = message }};

    public static Result<T> Fail(List<string> errors, string? message = null) =>
        new() {{ Success = false, Errors = errors, Message = message }};
}}

public class Result
{{
    public bool Success {{ get; private set; }}
    public string? Message {{ get; private set; }}
    public List<string>? Errors {{ get; private set; }}

    private Result() {{ }}

    public static Result Ok(string? message = null) =>
        new() {{ Success = true, Message = message }};

    public static Result Fail(List<string> errors, string? message = null) =>
        new() {{ Success = false, Errors = errors, Message = message }};
}}
";

        public string LoginRequest(string projectName) => $@"
namespace {projectName}.Application.DTOs.Auth;

public class LoginRequest
{{
    public string Email {{ get; set; }} = default!;
    public string Password {{ get; set; }} = default!;
}}
";

        /// <summary>New: RegisterRequest DTO with proper fields.</summary>
        public string RegisterRequest(string projectName) => $@"
namespace {projectName}.Application.DTOs.Auth;

public class RegisterRequest
{{
    public string Email {{ get; set; }} = default!;
    public string Password {{ get; set; }} = default!;
    public string? FirstName {{ get; set; }}
    public string? LastName {{ get; set; }}
}}
";

        public string LoginRequestValidator(string projectName) => $@"
using FluentValidation;
using {projectName}.Application.DTOs.Auth;

namespace {projectName}.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{{
    public LoginRequestValidator()
    {{
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }}
}}
";

        /// <summary>New: RegisterRequest validator.</summary>
        public string RegisterRequestValidator(string projectName) => $@"
using FluentValidation;
using {projectName}.Application.DTOs.Auth;

namespace {projectName}.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{{
    public RegisterRequestValidator()
    {{
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Matches(""[A-Z]"").WithMessage(""Password must contain at least one uppercase letter."")
            .Matches(""[0-9]"").WithMessage(""Password must contain at least one number."");
        RuleFor(x => x.FirstName).MaximumLength(100);
        RuleFor(x => x.LastName).MaximumLength(100);
    }}
}}
";

        public string DbSeeder(string projectName) => $@"
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using {projectName}.Domain.Entities;

namespace {projectName}.Infrastructure.Identity;

public static class DbSeeder
{{
    public static async Task SeedAsync(IServiceProvider services)
    {{
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var role in new[] {{ ""Admin"", ""User"" }})
        {{
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }}

        const string adminEmail = ""{_opts.DefaultAdminEmail}"";
        const string adminPassword = ""{_opts.DefaultAdminPassword}"";

        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {{
            var adminUser = new ApplicationUser
            {{
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            }};

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(adminUser, ""Admin"");
        }}
    }}
}}
";
    }
}
