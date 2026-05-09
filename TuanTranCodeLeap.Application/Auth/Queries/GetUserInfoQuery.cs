using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TuanTranCodeLeap.Application.Common.Exceptions;
using TuanTranCodeLeap.Application.DTOs;
using TuanTranCodeLeap.Domain.Entities;

namespace TuanTranCodeLeap.Application.Auth.Queries;

public record GetUserInfoQuery : IRequest<UserInfoDto>
{
    public int UserId { get; set; }
}

public class GetUserInfoQueryHandler : IRequestHandler<GetUserInfoQuery, UserInfoDto>
{
    private readonly UserManager<User> _userManager;
    private readonly ILogger<GetUserInfoQueryHandler> _logger;

    public GetUserInfoQueryHandler(UserManager<User> userManager, ILogger<GetUserInfoQueryHandler> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<UserInfoDto> Handle(GetUserInfoQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Retrieving user info for User ID: {UserId}", request.UserId);

            var user = await _userManager.FindByIdAsync(request.UserId.ToString());
            if (user == null)
            {
                _logger.LogWarning("User info retrieval failed: User not found - ID: {UserId}", request.UserId);
                throw new NotFoundException(nameof(User), request.UserId);
            }

            var roles = await _userManager.GetRolesAsync(user);

            _logger.LogInformation("User info retrieved successfully - ID: {UserId}, Email: {Email}", user.Id, user.Email);

            return new UserInfoDto
            {
                Id = user.Id,
                Email = user.Email!,
                FirstName = user.FirstName!,
                LastName = user.LastName!,
                Roles = roles.ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user info for User ID: {UserId}", request.UserId);
            throw;
        }
    }
}
