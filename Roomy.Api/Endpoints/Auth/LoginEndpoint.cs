using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Roomy.Api.Data;
using Roomy.Api.Services;
using BCrypt.Net;

namespace Roomy.Api.Endpoints.Auth;

public class LoginEndpoint : Endpoint<LoginRequest, LoginResponse>
{
    private readonly RoomyDbContext _dbContext;
    private readonly ITokenService _tokenService;

    public LoginEndpoint(RoomyDbContext dbContext, ITokenService tokenService)
    {
        _dbContext = dbContext;
        _tokenService = tokenService;
    }

    public override void Configure()
    {
        Post("/api/auth/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == req.Email, ct);

        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        {
            ThrowError("Invalid email or password", 401);
        }

        if (!user.IsActive)
        {
            ThrowError("Account is inactive", 403);
        }

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        var token = _tokenService.GenerateToken(user);

        Response = new LoginResponse
        {
            Token = token,
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString()
            }
        };
    }
}
