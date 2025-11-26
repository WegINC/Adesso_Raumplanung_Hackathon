using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Roomy.Api.Data;
using System.Security.Claims;

namespace Roomy.Api.Endpoints.Auth;

public class GetCurrentUserEndpoint : EndpointWithoutRequest<UserInfo>
{
    private readonly RoomyDbContext _dbContext;

    public GetCurrentUserEndpoint(RoomyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Get("/api/auth/me");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            ThrowError("Unauthorized", 401);
            return;
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user == null)
        {
            ThrowError("User not found", 404);
            return;
        }

        Response = new UserInfo
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString()
        };
    }
}
