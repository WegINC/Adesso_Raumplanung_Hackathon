# Roomy - Room Reservation System API

A .NET 10 Web API for managing room reservations using FastEndpoints and Entity Framework Core with SQL Server.

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
??? Endpoints/
?   ??? Auth/                      # Authentication endpoints
?   ?   ??? LoginEndpoint.cs
?   ?   ??? Login.Models.cs
?   ?   ??? RegisterEndpoint.cs
?   ?   ??? Register.Models.cs
?   ?   ??? GetCurrentUserEndpoint.cs
?   ??? Greeting/                  # Sample greeting endpoint
?   ?   ??? GreetingEndpoint.cs
?   ?   ??? GreetingRequest.cs
?   ?   ??? GreetingResponse.cs
?   ??? Rooms/                     # Room CRUD endpoints
?       ??? GetRoomsEndpoint.cs
?       ??? GetRoomsEndpoint.Models.cs
??? Services/
?   ??? TokenService.cs            # JWT token generation
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
5. **Test protected endpoints** like `/api/auth/me`

## API Endpoints

### Authentication

#### Register
- **POST** `/api/auth/register` - Register a new user
  ```json
  {
    "email": "user@example.com",
    "password": "Password123!",
    "firstName": "John",
    "lastName": "Doe"
  }
  ```

#### Login
- **POST** `/api/auth/login` - Login and receive JWT token
  ```json
  {
    "email": "user@example.com",
    "password": "Password123!"
  }
  ```
  
  Response:
  ```json
  {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "user": {
      "id": 1,
      "email": "user@example.com",
      "firstName": "John",
      "lastName": "Doe",
      "role": "User"
    }
  }
  ```

#### Get Current User
- **GET** `/api/auth/me` - Get current authenticated user
  - Requires: `Authorization: Bearer {token}` header

### Greeting (Sample)
- **GET** `/api/greeting/{name}` - Returns a personalized greeting message

### Rooms
- **GET** `/api/rooms` - Get all rooms

## Seeded Users

The application seeds the following users on first run:

| Email | Password | Role | Name |
|-------|----------|------|------|
| admin@roomy.com | Admin123! | Administrator | Admin User |
| john.doe@roomy.com | User123! | User | John Doe |
| jane.smith@roomy.com | User123! | User | Jane Smith |
| bob.wilson@roomy.com | User123! | User | Bob Wilson |
| alice.brown@roomy.com | User123! | User | Alice Brown |

## Entities

### User Entity

| Property | Type | Description |
|----------|------|-------------|
| Id | int | Primary key (auto-increment) |
| Email | string | User email (unique, required, max 256 chars) |
| PasswordHash | string | BCrypt hashed password |
| FirstName | string | First name (required, max 100 chars) |
| LastName | string | Last name (required, max 100 chars) |
| Role | UserRole | User role (User or Administrator) |
| IsActive | bool | Account status (default: true) |
| CreatedAt | DateTime | Creation timestamp (UTC) |
| LastLoginAt | DateTime? | Last login timestamp (UTC) |

### Room Entity

| Property | Type | Description |
|----------|------|-------------|
| Id | int | Primary key (auto-increment) |
| Name | string | Room name (required, max 100 chars) |
| Description | string? | Optional room description (max 500 chars) |
| Capacity | int | Maximum number of people |
| IsAvailable | bool | Availability status (default: true) |
| CreatedAt | DateTime | Creation timestamp (UTC) |
| UpdatedAt | DateTime? | Last update timestamp (UTC) |

## Authentication

This API uses **JWT (JSON Web Token)** authentication. To access protected endpoints:

1. **Login** using the `/api/auth/login` endpoint to receive a token
2. **Include the token** in subsequent requests using the Authorization header:
   ```
   Authorization: Bearer {your-token}
   ```

The token expires after 60 minutes (configurable in `appsettings.json`).

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

## Next Steps

To extend this API, you can:

1. **Add more Room CRUD endpoints**:
   - POST `/api/rooms` - Create a new room (Admin only)
   - GET `/api/rooms/{id}` - Get a specific room
   - PUT `/api/rooms/{id}` - Update a room (Admin only)
   - DELETE `/api/rooms/{id}` - Delete a room (Admin only)

2. **Add Reservation entity** for booking rooms with:
   - User association
   - Time slot management
   - Room availability checks

3. **Add role-based authorization** using FastEndpoints policies

4. **Add validation** using FluentValidation (built into FastEndpoints)

5. **Add email verification** for new user registrations

6. **Add password reset** functionality

## Testing the API

### Option 1: Swagger UI (Recommended)
Navigate to `https://localhost:5001/swagger` for interactive API testing.

### Option 2: HTTP File
Use the included `Roomy.Api.http` file with Visual Studio or VS Code REST Client extension.

### Option 3: cURL

#### Register
```bash
curl -X POST https://localhost:5001/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!","firstName":"Test","lastName":"User"}'
```

#### Login
```bash
curl -X POST https://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@roomy.com","password":"Admin123!"}'
```

#### Get Current User (with token)
```bash
curl -X GET https://localhost:5001/api/auth/me \
  -H "Authorization: Bearer {your-token}"
```

## FastEndpoints Resources

- [FastEndpoints Documentation](https://fast-endpoints.com/)
- [FastEndpoints GitHub](https://github.com/FastEndpoints/FastEndpoints)
- [FastEndpoints Swagger](https://fast-endpoints.com/docs/swagger-support)
