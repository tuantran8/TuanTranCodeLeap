using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using TuanTranCodeLeap.API.Common;
using TuanTranCodeLeap.API.Middleware;
using TuanTranCodeLeap.Application.Auth;
using TuanTranCodeLeap.Application.Constants;
using TuanTranCodeLeap.Application.Common.Behaviors;
using TuanTranCodeLeap.Domain.Entities;
using TuanTranCodeLeap.Infrastructure.Auth;
using TuanTranCodeLeap.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        // Custom validation error response
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .SelectMany(e => e.Value!.Errors.Select(err => $"{e.Key}: {err.ErrorMessage}"))
                .ToList();

            var response = new ApiResponse<object>
            {
                Status = false,
                Message = "Validation failed",
                Data = errors
            };

            return new BadRequestObjectResult(response);
        };
    });

// Configure Entity Framework Core
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure ASP.NET Identity with custom tables
builder.Services.AddIdentity<User, Role>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (jwtSettings != null)
{
    builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
    builder.Services.AddScoped<IJwtService, JwtService>();

    var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };

        // Custom 401 Unauthorized response and logging
        options.Events = new JwtBearerEvents
        {
            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                // Log the error for debugging
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning("Authentication failed. Error: {Error}, Description: {Description}",
                    context.Error,
                    context.ErrorDescription);

                var response = new ApiResponse<object>
                {
                    Status = false,
                    Message = $"{MessageConstants.Unauthorized} - {context.ErrorDescription ?? "Please provide a valid token"}",
                    Data = null
                };
                return context.Response.WriteAsJsonAsync(response);
            },
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogError(context.Exception, "Token validation failed: {Message}", context.Exception.Message);
                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

                // Check if token has been revoked
                var jti = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                if (!string.IsNullOrEmpty(jti))
                {
                    var revokedTokenRepo = context.HttpContext.RequestServices.GetRequiredService<TuanTranCodeLeap.Domain.Repositories.IRevokedTokenRepository>();
                    var isRevoked = await revokedTokenRepo.IsRevokedAsync(jti);
                    if (isRevoked)
                    {
                        logger.LogWarning("Token has been revoked. JTI: {Jti}", jti);
                        context.Fail("Token has been revoked");
                        return;
                    }
                }

                logger.LogInformation("Token validated successfully for user: {User}",
                    context.Principal?.Identity?.Name ?? "unknown");
            }
        };
    });
}

// Configure MediatR with CQRS
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblies(
        typeof(Program).Assembly,
        typeof(TuanTranCodeLeap.Application.Queries.Products.CreateProductCommand).Assembly,
        typeof(TuanTranCodeLeap.Application.Handlers.Products.CreateProductCommandHandler).Assembly
    );
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// Configure FluentValidation
builder.Services.AddValidatorsFromAssemblies(new[]
{
    typeof(Program).Assembly,
    typeof(TuanTranCodeLeap.Application.Queries.Products.CreateProductCommand).Assembly
});

// Configure Repositories
builder.Services.AddScoped<TuanTranCodeLeap.Domain.Repositories.IProductRepository, TuanTranCodeLeap.Infrastructure.Repositories.ProductRepository>();
builder.Services.AddScoped<TuanTranCodeLeap.Domain.Repositories.IUserRepository, TuanTranCodeLeap.Infrastructure.Repositories.UserRepository>();
builder.Services.AddScoped<TuanTranCodeLeap.Domain.Repositories.IRefreshTokenRepository, TuanTranCodeLeap.Infrastructure.Repositories.RefreshTokenRepository>();
builder.Services.AddScoped<TuanTranCodeLeap.Domain.Repositories.IRevokedTokenRepository, TuanTranCodeLeap.Infrastructure.Repositories.RevokedTokenRepository>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure API Explorer
builder.Services.AddEndpointsApiExplorer();

// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "TuanTranCodeLeap API",
        Version = "v1",
        Description = "A comprehensive API for product management with JWT authentication",
        Contact = new()
        {
            Name = "Tuan Tran",
            Email = "support@tuantran.com"
        },
        License = new()
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Add JWT Bearer Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new()
    {
        Name = "Authorization",
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {your_token}'",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    // Only apply security requirements to endpoints with [Authorize] attribute
    c.OperationFilter<AuthorizeCheckOperationFilter>();

    // Include XML comments if they exist
    var xmlFile = Path.Combine(AppContext.BaseDirectory, "TuanTranCodeLeap.API.xml");
    if (File.Exists(xmlFile))
    {
        c.IncludeXmlComments(xmlFile);
    }
});

// Configure Output Caching
builder.Services.AddOutputCache();

// Configure Memory Cache
builder.Services.AddMemoryCache();

var app = builder.Build();

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
    await DataSeeder.SeedProductsAsync(context);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "Docker")
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TuanTranCodeLeap API v1"));
}

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseOutputCache();

app.UseCors("AllowAll");

// Only use HTTPS redirection in non-development environments
// In development, Swagger uses HTTP which causes auth header loss on redirect
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

try
{
    Log.Information("Starting TuanTranCodeLeap API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
// configurationcanbeddd lt