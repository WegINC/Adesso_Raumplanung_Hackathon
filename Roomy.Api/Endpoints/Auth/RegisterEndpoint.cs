using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Roomy.Api.Data;
using Roomy.Api.Entities;
using BCrypt.Net;

namespace Roomy.Api.Endpoints.Auth;

public class RegisterEndpoint : Endpoint<RegisterRequest, RegisterResponse>
{
    private readonly RoomyDbContext _dbContext;

    public RegisterEndpoint(RoomyDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public override void Configure()
    {
        Post("/api/auth/register");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        // Check if user already exists
        var existingUser = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == req.Email, ct);

        if (existingUser != null)
        {
            ThrowError("Email already registered", 409);
        }

        // Create new user
        var user = new User
        {
            Email = req.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            FirstName = req.FirstName,
            LastName = req.LastName,
            Role = UserRole.User,
            IsActive = true
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(ct);

        Response = new RegisterResponse
        {
            Message = "Registration successful",
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
