# Activation Code API

A simple .NET 7 API service for validating activation codes with LiteDB storage.

## Features

- Validate activation codes via REST API
- Mark codes as used with 7-day expiration
- Validation attempt limits (max 3 attempts)
- Automatic cleanup of expired codes (runs hourly)
- LiteDB NoSQL database for local storage
- Pre-seeded test codes
- Automatic version bumping on commits
- JWT authentication for admin endpoints
- Rate limiting (3 req/min, 10 req/hour)
- CORS support
- Kubernetes health checks

## Quick Setup

### Install Git Hooks (for auto-versioning)

```bash
./scripts/install-hooks.sh
```

This installs a pre-commit hook that automatically increments the version by 0.0.1 on every commit.

## Running the API

### Local Development

```bash
cd ActivationCodeApi
dotnet restore
dotnet run
```

The API will be available at `https://localhost:7xxx` or `http://localhost:5xxx`

### Docker Deployment

**Quick start with Docker Compose:**
```bash
cd ActivationCodeApi
docker-compose up -d
```

The API will be available at `http://localhost:8080`

**Or build and run with Docker:**
```bash
docker build -t activationcode-api .
docker run -d -p 8080:8080 -v $(pwd)/data:/app/data activationcode-api
```

📖 **See [DEPLOYMENT.md](DEPLOYMENT.md) for complete deployment guide including:**
- Production configuration
- Environment variables
- Reverse proxy setup
- Backup strategies
- Troubleshooting

## Rate Limiting

The API enforces the following rate limits per IP address:
- **3 requests per minute**
- **10 requests per hour**

Exceeding these limits will return a `429 Too Many Requests` response.

## API Endpoint

### POST /api/activation/validate

Validates an activation code. If valid and unused, marks it as used and sets a 7-day expiration.

**Request:**
```json
{
  "code": "TEST-CODE-001"
}
```

**Response (Success - First Use):**
```json
{
  "isValid": true,
  "message": "Activation code successfully activated",
  "expiresAt": "2025-11-24T12:00:00Z"
}
```

**Response (Success - Already Used but Not Expired):**
```json
{
  "isValid": true,
  "message": "Activation code is valid",
  "expiresAt": "2025-11-24T12:00:00Z"
}
```

**Response (Error - Code Not Found):**
```json
{
  "isValid": false,
  "message": "Activation code not found"
}
```

**Response (Error - Expired):**
```json
{
  "isValid": false,
  "message": "Activation code has expired"
}
```

## Test Codes

The following codes are pre-seeded in the database:
- TEST-CODE-001
- TEST-CODE-002
- TEST-CODE-003
- DEMO-CODE-123
- DEMO-CODE-456

## Admin Authentication

### Default Credentials

On the **first run**, the application automatically creates an admin account with default credentials:
- **Username:** `admin`
- **Password:** `admin`

**IMPORTANT:** Change the password immediately after first login using the change password endpoint!

### POST /api/admin/login

Login to get a JWT authentication token (valid for 30 minutes).

**Request:**
```json
{
  "username": "admin",
  "password": "admin"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Login successful",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

### POST /api/admin/change-password

Change the admin password (requires authentication).

**Request:**
```json
{
  "oldPassword": "admin",
  "newPassword": "your-new-secure-password"
}
```

**Response:**
```json
{
  "message": "Password changed successfully"
}
```

## Admin Endpoints

All admin endpoints (except `/api/admin/login`) require authentication via the `Authorization` header with a Bearer token.

### POST /api/admin/generate-codes
Generate new activation codes (supports up to 20,000 codes per request).

**Request:**
```json
{
  "count": 1000,
  "prefix": "PROD"
}
```

**Response (small batches ≤100):**
```json
{
  "message": "Successfully generated 10 activation codes",
  "count": 10,
  "prefix": "PROD",
  "codes": ["PROD-A1B2C3D4E5F6", "PROD-G7H8I9J0K1L2", ...]
}
```

**Response (large batches >100):**
```json
{
  "message": "Successfully generated 5000 activation codes",
  "count": 5000,
  "prefix": "PROD",
  "note": "Use GET /api/admin/codes to retrieve the generated codes"
}
```

**Notes:**
- Maximum 20,000 codes per request
- Large batches are processed in chunks of 1,000 for optimal performance
- Codes ≤100 are returned in response; larger batches require fetching via GET endpoint

### GET /api/admin/codes
List all activation codes with skip token pagination.

**Query parameters:**
- `isUsed` (optional): Filter by used/unused status (true/false)
- `skipToken` (optional): ID of last seen code for pagination
- `pageSize` (default: 100, max: 1000): Number of codes per page

**Response:**
```json
{
  "codes": [
    {
      "id": 1,
      "code": "TEST-CODE-001",
      "isUsed": false,
      "activatedAt": null,
      "expiresAt": null
    }
  ],
  "totalCount": 5000,
  "pageSize": 100,
  "nextSkipToken": 100,
  "hasMore": true
}
```

**Pagination example:**
1. First request: `GET /api/admin/codes?pageSize=100`
2. Next request: `GET /api/admin/codes?pageSize=100&skipToken=100`
3. Continue using `nextSkipToken` from response until `hasMore` is false

### GET /api/admin/stats
Get statistics about activation codes.

**Response:**
```json
{
  "totalCodes": 100,
  "unusedCodes": 75,
  "usedCodes": 25,
  "activeCodes": 20
}
```

### DELETE /api/admin/codes/{code}
Delete a specific activation code.

### DELETE /api/admin/codes/expired
Delete all expired activation codes.

## Testing with curl

**Validate activation code (public endpoint):**
```bash
curl -X POST https://localhost:7xxx/api/activation/validate \
  -H "Content-Type: application/json" \
  -d '{"code":"TEST-CODE-001"}'
```

**Admin login:**
```bash
curl -X POST https://localhost:7xxx/api/admin/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin"}'
```

**Change password (admin - requires token):**
```bash
TOKEN="your-jwt-token-from-login"
curl -X POST https://localhost:7xxx/api/admin/change-password \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"oldPassword":"admin","newPassword":"NewSecurePass123"}'
```

**Generate codes (admin - requires token):**
```bash
# Small batch
curl -X POST https://localhost:7xxx/api/admin/generate-codes \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"count":50,"prefix":"NEW"}'

# Large batch
curl -X POST https://localhost:7xxx/api/admin/generate-codes \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"count":10000,"prefix":"BULK"}'
```

**Get codes with pagination (admin - requires token):**
```bash
# First page
curl -X GET "https://localhost:7xxx/api/admin/codes?pageSize=100" \
  -H "Authorization: Bearer $TOKEN"

# Next page using skipToken from previous response
curl -X GET "https://localhost:7xxx/api/admin/codes?pageSize=100&skipToken=100" \
  -H "Authorization: Bearer $TOKEN"

# Filter unused codes only
curl -X GET "https://localhost:7xxx/api/admin/codes?isUsed=false&pageSize=100" \
  -H "Authorization: Bearer $TOKEN"
```

**Get stats (admin - requires token):**
```bash
curl -X GET https://localhost:7xxx/api/admin/stats \
  -H "Authorization: Bearer $TOKEN"
```

## Database Management

The database is **automatically initialized on startup**:
- Creates SQLite database file (`activationcodes.db`) if it doesn't exist
- Creates all required tables (ActivationCodes, AdminUsers)
- Generates admin account on first run
- Seeds test activation codes

## Background Service

The `CodeCleanupService` runs every hour to automatically delete expired activation codes from the database.

## Security

- Default admin credentials (admin/admin) created on first run
- **Change the default password immediately after first login!**
- Passwords stored in SQLite with SHA-256 hashing
- Admin endpoints protected with JWT token authentication
- JWT tokens expire after 30 minutes
- Rate limiting prevents brute force attacks (3 req/min, 10 req/hour)
- Change JWT secret key in production via `appsettings.json`
