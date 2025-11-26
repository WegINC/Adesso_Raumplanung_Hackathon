# Swagger UI Quick Start Guide

## ?? Accessing Swagger

Once your application is running, navigate to:

```
https://localhost:5001/swagger
```

## ?? What You'll See

The Swagger UI displays:
- **Roomy API v1** - API title and version
- **Description**: Room Reservation System API with JWT Authentication
- **All available endpoints** organized by tags
- **Authorize button** (?? icon) at the top right

## ?? Testing with Authentication

### Step-by-Step Guide

#### 1?? Login to Get Token

1. Find the **POST /api/auth/login** endpoint
2. Click **"Try it out"**
3. Enter credentials in the request body:
   ```json
   {
     "email": "admin@roomy.com",
     "password": "Admin123!"
   }
   ```
4. Click **"Execute"**
5. Scroll down to see the response
6. **Copy the entire token value** from the response (just the token string, not the quotes)

Example token:
```
eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxIiwiZW1haWwiOiJhZG1pbkByb29teS5jb20iLCJnaXZlbl9uYW1lIjoiQWRtaW4iLCJmYW1pbHlfbmFtZSI6IlVzZXIiLCJyb2xlIjoiQWRtaW5pc3RyYXRvciIsIm5iZiI6MTcwMDAwMDAwMCwiZXhwIjoxNzAwMDAzNjAwLCJpc3MiOiJSb29teUFwaSIsImF1ZCI6IlJvb215Q2xpZW50In0.xxxxxxxxxxxxxxxxxxxxx
```

#### 2?? Authorize in Swagger

1. Click the **"Authorize"** button at the top of the page (?? icon)
2. A dialog will appear with a field labeled **"Value"**
3. Enter: `Bearer ` followed by your token
   ```
   Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```
   ?? **Important**: Don't forget the word "Bearer" and the space after it!
4. Click **"Authorize"**
5. The dialog will close and the lock icon changes to ?? (locked)
6. Click **"Close"**

#### 3?? Test Protected Endpoints

Now you can test any protected endpoint:

1. Find **GET /api/auth/me**
2. Click **"Try it out"**
3. Click **"Execute"**
4. You should see your user information in the response

If you see a 401 Unauthorized error:
- Check that you included "Bearer " prefix
- Verify the token hasn't expired (60 minutes)
- Make sure you copied the entire token

## ?? Available Endpoints

### ?? Public Endpoints (No Authentication Required)

#### POST /api/auth/register
Register a new user account
```json
{
  "email": "newuser@example.com",
  "password": "Password123!",
  "firstName": "Jane",
  "lastName": "Doe"
}
```

#### POST /api/auth/login
Login and receive JWT token
```json
{
  "email": "admin@roomy.com",
  "password": "Admin123!"
}
```

#### GET /api/greeting/{name}
Test greeting endpoint
- Parameter: `name` (string)
- Example: `/api/greeting/Developer`

#### GET /api/rooms
Get all rooms (currently public)

### ?? Protected Endpoints (Requires Authentication)

#### GET /api/auth/me
Get current authenticated user information
- Requires: Valid JWT token
- Returns: User profile with id, email, first name, last name, role

## ?? Tips and Tricks

### Quick Test Users

| Email | Password | Role |
|-------|----------|------|
| admin@roomy.com | Admin123! | Administrator |
| john.doe@roomy.com | User123! | User |
| jane.smith@roomy.com | User123! | User |

### Common Issues

**? 401 Unauthorized**
- Solution: Make sure you're authorized (click Authorize button)
- Check: Token includes "Bearer " prefix
- Verify: Token hasn't expired (60 minutes)

**? 400 Bad Request**
- Solution: Check your request body format
- Verify: All required fields are included
- Ensure: Data types match schema

**? 409 Conflict (Register)**
- Solution: Email already exists
- Try: Use a different email address

### Token Expiration

Tokens expire after **60 minutes**. If your token expires:
1. Click **"Authorize"** button
2. Click **"Logout"**
3. Login again to get a new token
4. Re-authorize with the new token

### Schema Information

Click on "Schema" tab (next to "Example Value") to see:
- ? Required fields
- ?? Field types
- ?? Validation rules
- ?? Field descriptions

## ?? Testing Workflow

### Recommended Flow for First-Time Testing

1. **Register a new user** (optional)
   - `POST /api/auth/register`

2. **Login with admin account**
   - `POST /api/auth/login`
   - Email: `admin@roomy.com`
   - Password: `Admin123!`

3. **Copy the token**
   - From login response

4. **Authorize**
   - Click Authorize button
   - Enter: `Bearer {token}`

5. **Test protected endpoint**
   - `GET /api/auth/me`

6. **Test other endpoints**
   - `GET /api/rooms`
   - `GET /api/greeting/YourName`

## ?? API Base URL

Development: `https://localhost:5001`

All endpoints are relative to this base URL.

## ?? Additional Resources

- **Swagger/OpenAPI Spec**: Available at `/swagger/v1/swagger.json`
- **HTTP Test File**: Use `Roomy.Api.http` in VS Code or Visual Studio
- **API Documentation**: See `README.md` and `AUTHENTICATION.md`

## ?? Swagger UI Features

- **Try it out**: Test endpoints directly in the browser
- **Schemas**: View data models and validation rules
- **Responses**: See example responses with status codes
- **Authorization**: Built-in JWT authentication support
- **Download**: Export OpenAPI specification

---

**Happy Testing! ??**

If you encounter any issues, check the console logs or refer to `AUTHENTICATION.md` for more details.
