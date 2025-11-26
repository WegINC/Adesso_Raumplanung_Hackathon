using Microsoft.EntityFrameworkCore;
using Roomy.Api.Entities;
using BCrypt.Net;

namespace Roomy.Api.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RoomyDbContext>();

        // Ensure database is created and migrations are applied
        await context.Database.MigrateAsync();

        // Seed Users
        if (!await context.Users.AnyAsync())
        {
            var users = new[]
            {
                new User
                {
                    Email = "admin@roomy.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    FirstName = "Admin",
                    LastName = "User",
                    Role = UserRole.Administrator,
                    IsActive = true
                },
                new User
                {
                    Email = "john.doe@roomy.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                    FirstName = "John",
                    LastName = "Doe",
                    Role = UserRole.User,
                    IsActive = true
                },
                new User
                {
                    Email = "jane.smith@roomy.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                    FirstName = "Jane",
                    LastName = "Smith",
                    Role = UserRole.User,
                    IsActive = true
                },
                new User
                {
                    Email = "bob.wilson@roomy.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                    FirstName = "Bob",
                    LastName = "Wilson",
                    Role = UserRole.User,
                    IsActive = true
                },
                new User
                {
                    Email = "alice.brown@roomy.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
                    FirstName = "Alice",
                    LastName = "Brown",
                    Role = UserRole.User,
                    IsActive = true
                }
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
        }

        // Seed Rooms
        if (!await context.Rooms.AnyAsync())
        {
            var rooms = new[]
            {
                new Room
                {
                    Name = "Conference Room A",
                    Description = "Large conference room with video conferencing equipment",
                    Capacity = 20,
                    IsAvailable = true
                },
                new Room
                {
                    Name = "Meeting Room 1",
                    Description = "Small meeting room for team discussions",
                    Capacity = 6,
                    IsAvailable = true
                },
                new Room
                {
                    Name = "Board Room",
                    Description = "Executive board room with premium amenities",
                    Capacity = 12,
                    IsAvailable = true
                },
                new Room
                {
                    Name = "Training Room",
                    Description = "Large room equipped for training sessions",
                    Capacity = 30,
                    IsAvailable = false
                },
                new Room
                {
                    Name = "Huddle Space",
                    Description = "Quick meeting space for informal discussions",
                    Capacity = 4,
                    IsAvailable = true
                }
            };

            await context.Rooms.AddRangeAsync(rooms);
            await context.SaveChangesAsync();
        }
    }
}
