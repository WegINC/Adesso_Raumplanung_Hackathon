# Room Reservation System - Complete Guide

## ?? Overview

The Roomy API has been transformed into a complete room booking system with:
- ? **Reservation Management** - Create, view, update, and cancel bookings
- ? **Conflict Detection** - Automatic detection of scheduling conflicts
- ? **Availability Checking** - Check if rooms are available before booking
- ? **User Permissions** - Users can only manage their own reservations (admins can manage all)
- ? **Soft Deletion** - Cancelled reservations are marked as cancelled, not deleted

## ?? New Entities

### Reservation Entity

| Property | Type | Description |
|----------|------|-------------|
| Id | int | Primary key (auto-increment) |
| RoomId | int | Foreign key to Room |
| UserId | int | Foreign key to User |
| Title | string | Reservation title (required, max 200 chars) |
| Description | string? | Optional description (max 1000 chars) |
| StartTime | DateTime | Reservation start time (UTC) |
| EndTime | DateTime | Reservation end time (UTC) |
| Status | ReservationStatus | Confirmed or Cancelled |
| CreatedAt | DateTime | Creation timestamp |
| UpdatedAt | DateTime? | Last update timestamp |

### ReservationStatus Enum
- `Confirmed` (0) - Active reservation
- `Cancelled` (1) - Cancelled reservation

## ?? Services

### ReservationService

Provides conflict detection and availability checking:

```csharp
Task<bool> HasConflictAsync(int roomId, DateTime startTime, DateTime endTime, int? excludeReservationId = null)
Task<List<Reservation>> GetConflictingReservationsAsync(int roomId, DateTime startTime, DateTime endTime, int? excludeReservationId = null)
Task<bool> IsRoomAvailableAsync(int roomId, DateTime startTime, DateTime endTime, int? excludeReservationId = null)
```

**Conflict Detection Logic:**
- Checks if any confirmed reservation overlaps with the requested time slot
- Two time periods overlap if: `startTime < existingEndTime AND endTime > existingStartTime`
- Can exclude a specific reservation ID (useful for updates)

## ?? API Endpoints

### Check Availability

**POST** `/api/reservations/check-availability` ?? Public

Check if a room is available for a specific time slot.

**Request:**
```json
{
  "roomId": 1,
  "startTime": "2024-12-01T09:00:00Z",
  "endTime": "2024-12-01T11:00:00Z"
}
```

**Response (Available):**
```json
{
  "isAvailable": true,
  "message": "Room is available for the selected time slot"
}
```

**Response (Not Available):**
```json
{
  "isAvailable": false,
  "message": "Room is not available for the selected time slot",
  "conflicts": [
    {
      "id": 5,
      "title": "Team Meeting",
      "startTime": "2024-12-01T09:00:00Z",
      "endTime": "2024-12-01T10:30:00Z"
    }
  ]
}
```

---

### Create Reservation

**POST** `/api/reservations` ?? Authenticated

Create a new room reservation.

**Request:**
```json
{
  "roomId": 1,
  "title": "Team Meeting",
  "description": "Weekly team sync",
  "startTime": "2024-12-01T09:00:00Z",
  "endTime": "2024-12-01T10:00:00Z"
}
```

**Response:**
```json
{
  "id": 1,
  "roomId": 1,
  "roomName": "Conference Room A",
  "userId": 2,
  "userName": "John Doe",
  "title": "Team Meeting",
  "description": "Weekly team sync",
  "startTime": "2024-12-01T09:00:00Z",
  "endTime": "2024-12-01T10:00:00Z",
  "status": "Confirmed",
  "createdAt": "2024-11-26T10:00:00Z",
  "updatedAt": null
}
```

**Validations:**
- ? End time must be after start time
- ? Cannot create reservation in the past
- ? Room must exist
- ? No scheduling conflicts

**Errors:**
- `400` - Invalid time range or past date
- `404` - Room not found
- `409` - Scheduling conflict

---

### Get All Reservations

**GET** `/api/reservations` ?? Public

Get all confirmed reservations with optional filters.

**Query Parameters:**
- `roomId` (optional) - Filter by room
- `userId` (optional) - Filter by user
- `startDate` (optional) - Filter reservations ending on or after this date
- `endDate` (optional) - Filter reservations starting on or before this date

**Examples:**
```
GET /api/reservations
GET /api/reservations?roomId=1
GET /api/reservations?startDate=2024-12-01&endDate=2024-12-31
GET /api/reservations?roomId=1&startDate=2024-12-01
```

**Response:**
```json
{
  "reservations": [
    {
      "id": 1,
      "roomId": 1,
      "roomName": "Conference Room A",
      "userId": 2,
      "userName": "John Doe",
      "title": "Team Meeting",
      "description": "Weekly team sync",
      "startTime": "2024-12-01T09:00:00Z",
      "endTime": "2024-12-01T10:00:00Z",
      "status": "Confirmed",
      "createdAt": "2024-11-26T10:00:00Z",
      "updatedAt": null
    }
  ]
}
```

---

### Get My Reservations

**GET** `/api/reservations/my` ?? Authenticated

Get all reservations for the currently authenticated user.

**Response:** Same format as Get All Reservations

---

### Get Specific Reservation

**GET** `/api/reservations/{id}` ?? Public

Get details of a specific reservation.

**Response:** Single reservation object

---

### Update/Reschedule Reservation

**PUT** `/api/reservations/{id}` ?? Authenticated

Update a reservation's details or reschedule it.

**Request:** (All fields optional)
```json
{
  "title": "Updated Team Meeting",
  "description": "Updated description",
  "startTime": "2024-12-01T10:00:00Z",
  "endTime": "2024-12-01T11:00:00Z"
}
```

**Permissions:**
- ? User can update their own reservations
- ? Administrators can update any reservation

**Validations:**
- ? End time must be after start time (if time changed)
- ? Cannot reschedule to the past
- ? No scheduling conflicts (excludes current reservation)
- ? Cannot update cancelled reservations

**Errors:**
- `400` - Invalid data or cancelled reservation
- `403` - Not owner or admin
- `404` - Reservation not found
- `409` - Scheduling conflict

---

### Cancel Reservation

**DELETE** `/api/reservations/{id}` ?? Authenticated

Cancel a reservation (soft delete).

**Permissions:**
- ? User can cancel their own reservations
- ? Administrators can cancel any reservation

**Response:**
```json
{
  "message": "Reservation cancelled successfully"
}
```

**Note:** Cancelled reservations remain in the database with status `Cancelled` but are excluded from availability checks and listing queries.

**Errors:**
- `400` - Already cancelled
- `403` - Not owner or admin
- `404` - Reservation not found

---

## ?? Conflict Detection Examples

### Scenario 1: Exact Overlap
```
Existing: 09:00 - 11:00
New:      09:00 - 11:00
Result:   ? CONFLICT
```

### Scenario 2: Partial Overlap (Start)
```
Existing: 09:00 - 11:00
New:      10:00 - 12:00
Result:   ? CONFLICT
```

### Scenario 3: Partial Overlap (End)
```
Existing: 10:00 - 12:00
New:      09:00 - 11:00
Result:   ? CONFLICT
```

### Scenario 4: Contained Within
```
Existing: 09:00 - 12:00
New:      10:00 - 11:00
Result:   ? CONFLICT
```

### Scenario 5: Contains Existing
```
Existing: 10:00 - 11:00
New:      09:00 - 12:00
Result:   ? CONFLICT
```

### Scenario 6: No Overlap (Before)
```
Existing: 11:00 - 12:00
New:      09:00 - 10:00
Result:   ? NO CONFLICT
```

### Scenario 7: No Overlap (After)
```
Existing: 09:00 - 10:00
New:      11:00 - 12:00
Result:   ? NO CONFLICT
```

### Scenario 8: Adjacent Slots
```
Existing: 09:00 - 10:00
New:      10:00 - 11:00
Result:   ? NO CONFLICT
```

**Note:** Adjacent time slots (end time = next start time) are NOT considered conflicts.

---

## ?? Testing Workflow

### 1. Check Availability First
```http
POST /api/reservations/check-availability
{
  "roomId": 1,
  "startTime": "2024-12-01T09:00:00Z",
  "endTime": "2024-12-01T10:00:00Z"
}
```

### 2. Login to Get Token
```http
POST /api/auth/login
{
  "email": "john.doe@roomy.com",
  "password": "User123!"
}
```

### 3. Create Reservation
```http
POST /api/reservations
Authorization: Bearer {token}
{
  "roomId": 1,
  "title": "My Meeting",
  "startTime": "2024-12-01T09:00:00Z",
  "endTime": "2024-12-01T10:00:00Z"
}
```

### 4. View Your Reservations
```http
GET /api/reservations/my
Authorization: Bearer {token}
```

### 5. Reschedule
```http
PUT /api/reservations/1
Authorization: Bearer {token}
{
  "startTime": "2024-12-01T10:00:00Z",
  "endTime": "2024-12-01T11:00:00Z"
}
```

### 6. Cancel
```http
DELETE /api/reservations/1
Authorization: Bearer {token}
```

---

## ?? Best Practices

### For Frontend Developers

1. **Always Check Availability First**
   - Before showing the booking form, check if the slot is available
   - Display conflicting reservations to help users choose alternative times

2. **Handle Time Zones Properly**
   - API expects UTC times
   - Convert local times to UTC before sending
   - Convert UTC to local times when displaying

3. **Show Clear Error Messages**
   - `409 Conflict` = Room already booked
   - `400 Bad Request` = Invalid data (check error message)
   - `403 Forbidden` = Not authorized to modify this reservation

4. **Implement Optimistic UI**
   - Show immediate feedback
   - Revert on error

5. **Refresh After Operations**
   - Reload reservation list after create/update/delete
   - Update calendar views

### For Administrators

1. **Monitor Reservations**
   - Use filters to find reservations by room or date range
   - Check for unusual patterns (excessive bookings)

2. **Manage Conflicts**
   - Can cancel any reservation if needed
   - Can reschedule problematic bookings

### Database Maintenance

1. **Archive Old Reservations**
   - Consider archiving reservations older than 1 year
   - Keep cancelled reservations for audit trail

2. **Indexing**
   - Index on `(RoomId, StartTime, EndTime)` already exists
   - Consider additional indexes based on query patterns

---

## ?? Security Considerations

1. **Authentication Required for Mutations**
   - Creating, updating, and deleting reservations require authentication
   - Viewing is public (adjust if needed)

2. **Authorization Checks**
   - Users can only modify their own reservations
   - Administrators can modify any reservation

3. **Data Validation**
   - Time ranges validated
   - Past dates rejected
   - Conflicts prevented

4. **Soft Deletes**
   - Cancelled reservations preserved for audit trail
   - Can be restored if needed (add endpoint)

---

## ?? Database Schema

```sql
CREATE TABLE Reservations (
    Id INT PRIMARY KEY IDENTITY,
    RoomId INT NOT NULL FOREIGN KEY REFERENCES Rooms(Id),
    UserId INT NOT NULL FOREIGN KEY REFERENCES Users(Id),
    Title NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    StartTime DATETIME2 NOT NULL,
    EndTime DATETIME2 NOT NULL,
    Status INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NULL,
    INDEX IX_Reservations_RoomId_StartTime_EndTime (RoomId, StartTime, EndTime)
);
```

---

## ?? Next Steps

Consider adding these features:

1. **Recurring Reservations**
   - Daily, weekly, monthly patterns
   - Series management

2. **Approval Workflow**
   - Pending status for reservations requiring approval
   - Approval/rejection endpoints

3. **Notifications**
   - Email reminders
   - Reservation confirmations
   - Cancellation notices

4. **Room Features**
   - Equipment (projector, whiteboard, etc.)
   - Capacity warnings
   - Photo uploads

5. **Reporting**
   - Utilization statistics
   - Popular time slots
   - User booking patterns

6. **Calendar Integration**
   - ICS file export
   - Outlook/Google Calendar sync

7. **Waitlist**
   - Join waitlist for fully booked slots
   - Automatic notification when slot becomes available

---

## ?? Additional Resources

- **Swagger UI**: `https://localhost:5001/swagger`
- **HTTP Test File**: `Roomy.Api.http`
- **README**: `README.md`
- **Authentication Guide**: `AUTHENTICATION.md`
- **Swagger Guide**: `SWAGGER_GUIDE.md`
