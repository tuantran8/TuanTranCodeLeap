using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TuanTranCodeLeap.API.Common;
using TuanTranCodeLeap.Application.Auth;
using TuanTranCodeLeap.Application.Constants;
using TuanTranCodeLeap.Application.Auth.Commands;
using TuanTranCodeLeap.Application.Auth.Queries;
using TuanTranCodeLeap.Application.DTOs;
using TuanTranCodeLeap.Domain.Entities;
using TuanTranCodeLeap.Domain.Repositories;

namespace TuanTranCodeLeap.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public AccountController(IMediator mediator, IRefreshTokenRepository refreshTokenRepository)
    {
        _mediator = mediator;
        _refreshTokenRepository = refreshTokenRepository;
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse>> Logout()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(ApiResponse.Error(MessageConstants.InvalidUserIdentifier));
            }

            // Blacklist the current JWT token
            var jti = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
            var expClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Exp)?.Value;

            if (!string.IsNullOrEmpty(jti))
            {
                var revokedTokenRepo = HttpContext.RequestServices.GetRequiredService<IRevokedTokenRepository>();
                var expiresAt = !string.IsNullOrEmpty(expClaim) && long.TryParse(expClaim, out long exp)
                    ? DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime
                    : DateTime.UtcNow.AddMinutes(60);

                await revokedTokenRepo.AddAsync(new RevokedToken
                {
                    TokenJti = jti,
                    Token = Request.Headers.Authorization.ToString().Replace("Bearer ", ""),
                    UserId = userId,
                    RevokedAt = DateTime.UtcNow,
                    ExpiresAt = expiresAt,
                    Reason = "User logout"
                });
            }

            // Revoke all refresh tokens for this user
            var refreshTokens = await _refreshTokenRepository.GetByUserIdAsync(userId);
            foreach (var token in refreshTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
                await _refreshTokenRepository.UpdateAsync(token);
            }

            return Ok(ApiResponse.Success(MessageConstants.LogoutSuccess));
        }
        catch (Exception ex)
        {
            return StatusCode(500, ApiResponse.Error(string.Format(MessageConstants.ErrorOccurred, "logout", ex.Message)));
        }
    }

    [HttpGet("userinfo")]
    public async Task<ActionResult<UserInfoDto>> GetUserInfo()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid user identifier" });
            }

            var query = new GetUserInfoQuery { UserId = userId };
            var result = await _mediator.Send(query);
            
            if (result == null)
            {
                return NotFound(new { error = "User not found" });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred while retrieving user information", details = ex.Message });
        }
    }

    [HttpPost("refresh-token")]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new { error = "Refresh token is required" });
            }

            var refreshToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken);

            if (refreshToken == null)
            {
                return Unauthorized(new { error = "Invalid refresh token" });
            }

            if (refreshToken.IsRevoked || refreshToken.IsUsed)
            {
                return Unauthorized(new { error = "Refresh token has been revoked or used" });
            }

            if (refreshToken.Expires < DateTime.UtcNow)
            {
                return Unauthorized(new { error = "Refresh token has expired" });
            }

            // Mark the refresh token as used
            refreshToken.IsUsed = true;
            await _refreshTokenRepository.UpdateAsync(refreshToken);

            // Generate new JWT token
            var jwtService = HttpContext.RequestServices.GetRequiredService<IJwtService>();
            var userManager = HttpContext.RequestServices.GetRequiredService<UserManager<User>>();
            var roles = await userManager.GetRolesAsync(refreshToken.User!);
            var (newToken, expiresAt) = await jwtService.GenerateTokenAsync(refreshToken.User!);

            // Generate new refresh token
            var newRefreshToken = GenerateRefreshToken();
            var newRefreshTokenEntity = new RefreshToken
            {
                Token = newRefreshToken,
                UserId = refreshToken.UserId,
                Expires = DateTime.UtcNow.AddDays(TuanTranCodeLeap.Application.Constants.AuthConstants.RefreshTokenExpirationDays),
                IsUsed = false,
                IsRevoked = false,
                Created = DateTime.UtcNow
            };

            await _refreshTokenRepository.AddAsync(newRefreshTokenEntity);

            return Ok(new AuthResponseDto
            {
                Token = newToken,
                RefreshToken = newRefreshToken,
                Email = refreshToken.User!.Email!,
                FirstName = refreshToken.User.FirstName!,
                LastName = refreshToken.User.LastName!,
                Roles = roles.ToList(),
                ExpiresAt = expiresAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An error occurred during token refresh", details = ex.Message });
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
