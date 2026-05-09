using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TuanTranCodeLeap.Application.Constants;
using TuanTranCodeLeap.Application.DTOs;
using TuanTranCodeLeap.Domain.Entities;
using TuanTranCodeLeap.Domain.Repositories;

namespace TuanTranCodeLeap.Application.Auth.Commands;

public record LoginCommand : IRequest<AuthResponseDto>
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email is not valid");
        
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IJwtService _jwtService;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginCommandHandler(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IJwtService jwtService,
        ILogger<LoginCommandHandler> logger,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
        _logger = logger;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", request.Email);

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found - Email: {Email}", request.Email);
                throw new UnauthorizedAccessException(MessageConstants.LoginInvalid);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Login failed: Invalid password - Email: {Email}", request.Email);
                throw new UnauthorizedAccessException(MessageConstants.LoginInvalid);
            }

            var (token, expiresAt) = await _jwtService.GenerateTokenAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            // Generate refresh token
            var refreshToken = GenerateRefreshToken();
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                Expires = DateTime.UtcNow.AddDays(AuthConstants.RefreshTokenExpirationDays), // Refresh token expires in 7 days
                IsUsed = false,
                IsRevoked = false,
                Created = DateTime.UtcNow
            };

            // Remove old refresh tokens for this user (optional - keeps only one active token)
            var oldTokens = await _refreshTokenRepository.GetByUserIdAsync(user.Id);
            await _refreshTokenRepository.RemoveRangeAsync(oldTokens);

            await _refreshTokenRepository.AddAsync(refreshTokenEntity);

            _logger.LogInformation("Login successful for email: {Email}, User ID: {UserId}", request.Email, user.Id);

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                Email = user.Email!,
                FirstName = user.FirstName!,
                LastName = user.LastName!,
                Roles = roles.ToList(),
                ExpiresAt = expiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            throw;
        }
    }

    private string GenerateRefreshToken()
    {
        var random = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(random);
        return Convert.ToBase64String(random);
    }
}
