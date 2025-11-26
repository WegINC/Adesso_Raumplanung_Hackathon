# Roomy - Room Reservation System API

A .NET 10 Web API for complete room booking management using FastEndpoints and Entity Framework Core with SQL Server.

## ?? Features

- ? **User Authentication** - JWT-based authentication with role-based access
- ? **Room Management** - Browse available rooms with capacity information
- ? **Reservation System** - Create, view, update, and cancel room bookings
- ? **Conflict Detection** - Automatic scheduling conflict prevention
- ? **Availability Checking** - Real-time room availability verification
- ? **Permission Management** - Users manage their bookings, admins manage all
- ? **Interactive API Docs** - Full Swagger/OpenAPI documentation

## Technologies

- **.NET 10**
- **FastEndpoints** - Fast and lightweight alternative to MVC/Minimal APIs
- **FastEndpoints.Swagger** - OpenAPI/Swagger documentation
- **Entity Framework Core 10** - ORM for database access
- **SQL Server** - Database (LocalDB for development)
- **JWT Authentication** - Token-based authentication
- **BCrypt** - Password hashing

## Project Structure

```
Roomy.Api/
??? Configuration/
?   ??? JwtSettings.cs             # JWT configuration
??? Data/
?   ??? RoomyDbContext.cs          # EF Core database context
?   ??? DbInitializer.cs           # Database seeding
??? Entities/
?   ??? Room.cs                    # Room entity model
?   ??? User.cs                    # User entity model
?   ??? Reservation.cs             # Reservation entity model
??? Endpoints/
?   ??? Auth/                      # Authentication endpoints
?   ??? Greeting/                  # Sample greeting endpoint
?   ??? Rooms/                     # Room endpoints
?   ??? Reservations/              # Reservation endpoints
??? Services/
?   ??? TokenService.cs            # JWT token generation
?   ??? ReservationService.cs      # Conflict detection & availability
??? Migrations/                    # EF Core database migrations
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- SQL Server LocalDB (included with Visual Studio)

### Database Setup

1. **Update the database** (applies migrations):
   ```bash
   dotnet ef database update --project Roomy.Api
   ```

2. **View connection string** in `appsettings.json`:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=RoomyDb;Trusted_Connection=true;TrustServerCertificate=true;"
   }
   ```

### Running the Application

```bash
dotnet run --project Roomy.Api
```

The API will be available at `https://localhost:5001` (or the port shown in console).

## ?? API Documentation (Swagger)

Once the application is running, access the interactive API documentation at:

**Swagger UI**: `https://localhost:5001/swagger`

The Swagger UI provides:
- ?? Complete API endpoint documentation
- ?? JWT Bearer token authentication (click "Authorize" button)
- ?? Interactive testing of all endpoints
- ?? Request/response schemas and examples

### Using Swagger with Authentication

1. **Login** using the `/api/auth/login` endpoint in Swagger
2. **Copy the token** from the response
3. **Click "Authorize"** button at the top of Swagger UI
4. **Enter**: `Bearer {your-token}` (include "Bearer " prefix)
5. **Test protected endpoints** like creating reservations

## API Endpoints

### Authentication

- **POST** `/api/auth/register` - Register a new user
- **POST** `/api/auth/login` - Login and receive JWT token
- **GET** `/api/auth/me` ?? - Get current authenticated user

### Rooms

- **GET** `/api/rooms` - Get all rooms

### Reservations

- **POST** `/api/reservations/check-availability` - Check if room is available
- **POST** `/api/reservations` ?? - Create a new reservation
- **GET** `/api/reservations` - Get all reservations (with optional filters)
- **GET** `/api/reservations/my` ?? - Get your reservations
- **GET** `/api/reservations/{id}` - Get specific reservation
- **PUT** `/api/reservations/{id}` ?? - Update/reschedule reservation
- **DELETE** `/api/reservations/{id}` ?? - Cancel reservation

?? = Requires authentication

## Quick Start Example

### 1. Check if a room is available
```bash
POST /api/reservations/check-availability
{
  "roomId": 1,
  "startTime": "2024-12-01T09:00:00Z",
  "endTime": "2024-12-01T10:00:00Z"
}
```

### 2. Login
```bash
POST /api/auth/login
{
  "email": "john.doe@roomy.com",
  "password": "User123!"
}
```

### 3. Create Reservation
```bash
POST /api/reservations
Authorization: Bearer {token}
{
  "roomId": 1,
  "title": "Team Meeting",
  "description": "Weekly team sync",
  "startTime": "2024-12-01T09:00:00Z",
  "endTime": "2024-12-01T10:00:00Z"
}
```

## Seeded Data

### Users

| Email | Password | Role | Name |
|-------|----------|------|------|
| admin@roomy.com | Admin123! | Administrator | Admin User |
| john.doe@roomy.com | User123! | User | John Doe |
| jane.smith@roomy.com | User123! | User | Jane Smith |
| bob.wilson@roomy.com | User123! | User | Bob Wilson |
| alice.brown@roomy.com | User123! | User | Alice Brown |

### Rooms

- Conference Room A (Capacity: 20)
- Meeting Room 1 (Capacity: 6)
- Board Room (Capacity: 12)
- Training Room (Capacity: 30)
- Huddle Space (Capacity: 4)

## Entities

### User Entity

| Property | Type | Description |
|----------|------|-------------|
| Id | int | Primary key |
| Email | string | Unique user email |
| PasswordHash | string | BCrypt hashed password |
| FirstName | string | First name |
| LastName | string | Last name |
| Role | UserRole | User or Administrator |
| IsActive | bool | Account status |
| CreatedAt | DateTime | Creation timestamp |
| LastLoginAt | DateTime? | Last login timestamp |

### Room Entity

| Property | Type | Description |
|----------|------|-------------|
| Id | int | Primary key |
| Name | string | Room name |
| Description | string? | Optional description |
| Capacity | int | Maximum occupancy |
| CreatedAt | DateTime | Creation timestamp |
| UpdatedAt | DateTime? | Last update timestamp |

### Reservation Entity

| Property | Type | Description |
|----------|------|-------------|
| Id | int | Primary key |
| RoomId | int | Foreign key to Room |
| UserId | int | Foreign key to User |
| Title | string | Reservation title |
| Description | string? | Optional description |
| StartTime | DateTime | Reservation start (UTC) |
| EndTime | DateTime | Reservation end (UTC) |
| Status | ReservationStatus | Confirmed or Cancelled |
| CreatedAt | DateTime | Creation timestamp |
| UpdatedAt | DateTime? | Last update timestamp |

## Conflict Detection

The system automatically prevents double-booking:

```
Two reservations conflict if:
StartTime1 < EndTime2 AND EndTime1 > StartTime2
```

**Example Conflicts:**
- ? 09:00-10:00 and 10:00-11:00 (Adjacent - NO conflict)
- ? 09:00-11:00 and 10:00-12:00 (Overlap - CONFLICT)
- ? 09:00-12:00 and 10:00-11:00 (Contains - CONFLICT)

See `RESERVATIONS_GUIDE.md` for detailed conflict scenarios.

## Authentication

This API uses **JWT (JSON Web Token)** authentication. To access protected endpoints:

1. **Login** using the `/api/auth/login` endpoint to receive a token
2. **Include the token** in subsequent requests using the Authorization header:
   ```
   Authorization: Bearer {your-token}
   ```

The token expires after 60 minutes (configurable in `appsettings.json`).

## Permissions

### Users Can:
- ? Create their own reservations
- ? View all reservations
- ? Update their own reservations
- ? Cancel their own reservations

### Administrators Can:
- ? All user permissions
- ? Update any reservation
- ? Cancel any reservation

## Database Migrations

### Create a new migration
```bash
dotnet ef migrations add MigrationName --project Roomy.Api
```

### Apply migrations to database
```bash
dotnet ef database update --project Roomy.Api
```

### Remove last migration
```bash
dotnet ef migrations remove --project Roomy.Api
```

## Configuration

JWT settings can be configured in `appsettings.json`:

```json
"JwtSettings": {
  "Secret": "YourSecretKeyHere-MustBe32CharactersOrMore",
  "Issuer": "RoomyApi",
  "Audience": "RoomyClient",
  "ExpirationInMinutes": 60
}
```

?? **Important**: Change the JWT secret in production!

## Testing the API

### Option 1: Swagger UI (Recommended ?)
Navigate to `https://localhost:5001/swagger` for interactive API testing.

### Option 2: HTTP File
Use the included `Roomy.Api.http` file with Visual Studio or VS Code REST Client extension.

### Option 3: cURL or Postman
See examples in `RESERVATIONS_GUIDE.md`

## Documentation

- ?? **README.md** (this file) - Project overview
- ?? **RESERVATIONS_GUIDE.md** - Complete reservation system guide
- ?? **AUTHENTICATION.md** - Authentication implementation details
- ?? **SWAGGER_GUIDE.md** - Swagger UI usage guide

## Next Steps / Future Enhancements

Consider adding:

1. **Recurring Reservations** - Daily/weekly/monthly bookings
2. **Approval Workflow** - Require admin approval for reservations
3. **Email Notifications** - Reminders and confirmations
4. **Room Features** - Equipment, amenities, photos
5. **Calendar Integration** - ICS export, Outlook/Google sync
6. **Reporting** - Utilization statistics and analytics
7. **Waitlist** - Queue for fully booked time slots
8. **Room CRUD for Admins** - Add/edit/delete rooms

## Resources

- [FastEndpoints Documentation](https://fast-endpoints.com/)
- [FastEndpoints GitHub](https://github.com/FastEndpoints/FastEndpoints)
- [FastEndpoints Swagger](https://fast-endpoints.com/docs/swagger-support)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [JWT Authentication](https://jwt.io/)

---

**Built with ?? using .NET 10 and FastEndpoints**
