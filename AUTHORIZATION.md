# API Authorization Summary

This document describes the authorization model implemented across all API endpoints.

## ?? Authorization Levels

### 1. Public (Anonymous) Endpoints
These endpoints do not require authentication:

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/auth/register` | Register a new user account |
| POST | `/api/auth/login` | Login and receive JWT token |
| POST | `/api/reservations/check-availability` | Check room availability |

**Rationale**: These endpoints must be public to allow user registration, login, and availability checking for planning purposes.

### 2. Protected Endpoints (Authenticated Users)
These endpoints require a valid JWT token but are accessible to all authenticated users:

| Method | Endpoint | Purpose |
|--------|----------|---------|
| GET | `/api/auth/me` | Get current user information |
| GET | `/api/greeting/{name}` | Test greeting endpoint |
| GET | `/api/rooms` | Get all rooms |
| GET | `/api/rooms/{id}` | Get room details by ID |
| GET | `/api/reservations` | Get all reservations (with filters) |
| GET | `/api/reservations/{id}` | Get reservation details |
| GET | `/api/reservations/my` | Get current user's reservations |
| POST | `/api/reservations` | Create a new reservation |

**Rationale**: Users must be logged in to view rooms, reservations, and create their own reservations.

### 3. Resource Owner or Admin Endpoints
These endpoints require the user to be either the owner of the resource OR an administrator:

| Method | Endpoint | Authorization Logic |
|--------|----------|---------------------|
| PUT | `/api/reservations/{id}` | Owner OR Administrator |
| DELETE | `/api/reservations/{id}` | Owner OR Administrator |

**Implementation**:
```csharp
var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
if (reservation.UserId != userId && userRole != "Administrator")
{
    ThrowError("You don't have permission to update this reservation", 403);
    return;
}
```

### 4. Administrator-Only Endpoints
These endpoints require the Administrator role:

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/rooms` | Create a new room |
| PUT | `/api/rooms/{id}` | Update room details |
| DELETE | `/api/rooms/{id}` | Delete a room |

**Implementation**:
```csharp
Roles("Administrator");
```

## ?? Authorization Matrix

| Endpoint | Anonymous | User | Owner | Admin |
|----------|-----------|------|-------|-------|
| POST /api/auth/register | ? | ? | ? | ? |
| POST /api/auth/login | ? | ? | ? | ? |
| GET /api/auth/me | ? | ? | ? | ? |
| GET /api/greeting/{name} | ? | ? | ? | ? |
| POST /api/reservations/check-availability | ? | ? | ? | ? |
| GET /api/rooms | ? | ? | ? | ? |
| GET /api/rooms/{id} | ? | ? | ? | ? |
| POST /api/rooms | ? | ? | ? | ? |
| PUT /api/rooms/{id} | ? | ? | ? | ? |
| DELETE /api/rooms/{id} | ? | ? | ? | ? |
| GET /api/reservations | ? | ? | ? | ? |
| GET /api/reservations/{id} | ? | ? | ? | ? |
| GET /api/reservations/my | ? | ? | ? | ? |
| POST /api/reservations | ? | ? | ? | ? |
| PUT /api/reservations/{id} | ? | ? | ? | ? |
| DELETE /api/reservations/{id} | ? | ? | ? | ? |

## ?? JWT Token Claims

The JWT token includes the following claims:

```csharp
var claims = new[]
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim(ClaimTypes.Email, user.Email),
    new Claim(ClaimTypes.GivenName, user.FirstName),
    new Claim(ClaimTypes.Surname, user.LastName),
    new Claim(ClaimTypes.Role, user.Role.ToString())
};
```

## ??? Security Measures

### 1. Authentication
- JWT Bearer token authentication
- Token expiration: 60 minutes (configurable)
- Tokens must include "Bearer " prefix in Authorization header

### 2. Authorization Checks
- Role-based authorization using `Roles("Administrator")`
- Resource ownership validation
- User identity verification via `ClaimTypes.NameIdentifier`

### 3. Error Responses

| Status Code | Meaning | When It Occurs |
|-------------|---------|----------------|
| 401 Unauthorized | No valid token provided | Missing or invalid JWT token |
| 403 Forbidden | Insufficient permissions | User lacks required role or ownership |
| 404 Not Found | Resource doesn't exist | Invalid resource ID |
| 409 Conflict | Operation conflict | Room already booked, reservation already cancelled, etc. |

## ?? Testing Authorization

### Test as Regular User

1. **Login as regular user**:
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "john.doe@roomy.com",
  "password": "User123!"
}
```

2. **Can do**:
   - ? View rooms
   - ? View all reservations
   - ? Create own reservations
   - ? Update/delete own reservations

3. **Cannot do**:
   - ? Create/update/delete rooms
   - ? Update/delete other users' reservations

### Test as Administrator

1. **Login as administrator**:
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "admin@roomy.com",
  "password": "Admin123!"
}
```

2. **Can do**:
   - ? Everything a regular user can do
   - ? Create/update/delete rooms
   - ? Update/delete any user's reservations

## ?? Best Practices Implemented

1. **Principle of Least Privilege**: Users only have access to operations they need
2. **Ownership Validation**: Users can only modify their own resources (except admins)
3. **Role-Based Access Control**: Clear separation between User and Administrator roles
4. **Explicit Authorization**: Each endpoint explicitly declares its authorization requirements
5. **Consistent Error Handling**: Standard HTTP status codes for authorization failures
6. **Token-Based Authentication**: Stateless authentication using JWT

## ?? Common Authorization Patterns

### Pattern 1: Public Endpoint
```csharp
public override void Configure()
{
    Post("/api/auth/login");
    AllowAnonymous();
}
```

### Pattern 2: Authenticated Endpoint (Default)
```csharp
public override void Configure()
{
    Get("/api/rooms");
    // No AllowAnonymous() = requires authentication
}
```

### Pattern 3: Admin-Only Endpoint
```csharp
public override void Configure()
{
    Post("/api/rooms");
    Roles("Administrator");
}
```

### Pattern 4: Owner or Admin Endpoint
```csharp
public override async Task HandleAsync(DeleteReservationRequest req, CancellationToken ct)
{
    // Get current user
    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    
    // Get resource
    var reservation = await _dbContext.Reservations.FindAsync(req.Id);
    
    // Check ownership or admin role
    var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
    if (reservation.UserId != userId && userRole != "Administrator")
    {
        ThrowError("You don't have permission", 403);
        return;
    }
    
    // Proceed with operation
}
```

## ?? Migration from Previous State

### Changes Made:

1. **Greeting Endpoint**: Changed from `AllowAnonymous()` to authenticated
2. **Room Endpoints**: Changed GET endpoints from `AllowAnonymous()` to authenticated
3. **Reservation GET Endpoints**: Changed from `AllowAnonymous()` to authenticated
4. **Maintained**:
   - Check availability remains public (useful for planning)
   - Admin-only endpoints already properly configured
   - Owner/admin authorization already properly implemented

## ?? Usage Guidelines

### For Frontend Developers

1. **Store the token** after login
2. **Include token** in all API requests (except public endpoints):
   ```
   Authorization: Bearer {token}
   ```
3. **Handle 401 errors**: Redirect to login
4. **Handle 403 errors**: Show "permission denied" message
5. **Refresh tokens**: Implement token refresh when approaching expiration

### For API Consumers

1. **Always check status codes**: Don't assume success
2. **Respect rate limits**: Implement exponential backoff for retries
3. **Use HTTPS**: Never send tokens over unencrypted connections
4. **Don't hardcode tokens**: Use environment variables or secure storage
5. **Validate input**: Even though the API validates, don't send invalid data

---

**Last Updated**: 2024-12-26
**API Version**: 1.0
