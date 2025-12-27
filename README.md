# PulseAPI

"Know your API is down before your users do."

Monitors API uptime, latency, and failures with alerts when services go down.

## Architecture

- **Backend**: .NET Core Web API with worker service
- **Frontend**: React + TypeScript + Tailwind CSS
- **Database**: PostgreSQL
- **Background Jobs**: .NET Worker Service for periodic API health checks

## Prerequisites

- .NET 10 SDK
- Node.js 18+ and npm
- Docker and Docker Compose (for PostgreSQL)
- PostgreSQL (or use Docker Compose)

## Setup

### 1. Database Setup

Start PostgreSQL using Docker Compose:

```bash
docker-compose up -d
```

Or use an existing PostgreSQL instance and update the connection string in `backend/PulseAPI.Api/appsettings.json`.

### 2. Backend Setup

```bash
cd backend

# Restore packages
dotnet restore

# Run database migrations
dotnet ef database update --project PulseAPI.Infrastructure/PulseAPI.Infrastructure.csproj --startup-project PulseAPI.Api/PulseAPI.Api.csproj

# Run the API
dotnet run --project PulseAPI.Api/PulseAPI.Api.csproj
```

The API will be available at `https://localhost:7000` (or `http://localhost:5000`).

### 3. Worker Service Setup

In a separate terminal:

```bash
cd backend
dotnet run --project PulseAPI.Worker/PulseAPI.Worker.csproj
```

The worker service will periodically check APIs and evaluate alerts.

### 4. Frontend Setup

```bash
cd frontend/pulseapi-web

# Install dependencies
npm install

# Start development server
npm start
```

The frontend will be available at `http://localhost:3000`.

## Configuration

### Backend Configuration

Update `backend/PulseAPI.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=pulseapi;Username=postgres;Password=postgres"
  },
  "Jwt": {
    "Key": "YourSuperSecretKeyThatShouldBeAtLeast32CharactersLongForProductionUse!",
    "Issuer": "PulseAPI",
    "Audience": "PulseAPI",
    "ExpirationMinutes": "1440"
  }
}
```

**Important**: Change the JWT Key in production to a secure random string.

### Worker Service Configuration

The worker service uses the same connection string from `appsettings.json`. It runs health checks every 10 seconds and checks each API based on its configured `CheckIntervalSeconds`.

### Frontend Configuration

Create `.env` file in `frontend/pulseapi-web`:

```env
REACT_APP_API_URL=http://localhost:5000/api
```

For HTTPS backend:
```env
REACT_APP_API_URL=https://localhost:7000/api
```

### Environment Variables

#### Backend
- Connection strings can be overridden with environment variables
- JWT settings can be configured via environment variables or appsettings.json

#### Frontend
- `REACT_APP_API_URL` - Backend API base URL (default: http://localhost:5000/api)

## Features

### Core Features
- **API Monitoring**: Monitor multiple APIs with configurable check intervals
- **Health Checks**: Automatic HTTP health checks with configurable methods (GET, POST, PUT, DELETE, etc.)
- **Metrics Dashboard**: Real-time metrics including:
  - Total Traffic (TPS) - Transactions per second
  - Error Rate - Percentage of failed requests
  - Latency (P99) - 99th percentile latency in milliseconds
  - Active Alerts - Count of unresolved alerts
- **Alerts**: Configure alerts based on:
  - Error Rate thresholds
  - Latency thresholds
  - Status Code matching
  - Uptime monitoring
- **Collections**: Group APIs together for monitoring and collection-level alerts
- **Authentication**: JWT-based authentication with secure password hashing

### Advanced Features
- **Environment Filtering**: Filter APIs and metrics by environment (prod, staging, dev)
- **Historical Data**: View health check history with pagination
- **Alert History**: Track when alerts were fired and resolve them
- **Custom Headers**: Configure custom HTTP headers for API checks
- **Request Body**: Support for POST/PUT requests with custom body
- **Timeout Configuration**: Configurable timeout per API
- **Real-time Updates**: Dashboard auto-refreshes every 30 seconds

## Quick Start

1. **Start PostgreSQL**:
   ```bash
   docker-compose up -d
   ```

2. **Set up and run the backend**:
   ```bash
   cd backend
   dotnet restore
   dotnet ef database update --project PulseAPI.Infrastructure/PulseAPI.Infrastructure.csproj --startup-project PulseAPI.Api/PulseAPI.Api.csproj
   dotnet run --project PulseAPI.Api/PulseAPI.Api.csproj
   ```

3. **Start the worker service** (in a new terminal):
   ```bash
   cd backend
   dotnet run --project PulseAPI.Worker/PulseAPI.Worker.csproj
   ```

4. **Set up and run the frontend** (in a new terminal):
   ```bash
   cd frontend/pulseapi-web
   npm install
   npm start
   ```

5. **Access the application**:
   - Frontend: http://localhost:3000
   - API: https://localhost:7000
   - Swagger UI: https://localhost:7000/swagger

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register a new user
  ```json
  {
    "email": "user@example.com",
    "password": "password123",
    "firstName": "John",
    "lastName": "Doe"
  }
  ```
- `POST /api/auth/login` - Login and get JWT token
  ```json
  {
    "email": "user@example.com",
    "password": "password123"
  }
  ```
- `GET /api/auth/me` - Get current authenticated user

### APIs Management
- `GET /api/apis?environment=prod` - List all APIs (optional environment filter)
- `GET /api/apis/{id}` - Get API by ID
- `POST /api/apis` - Create a new API to monitor
  ```json
  {
    "name": "My API",
    "url": "https://api.example.com/health",
    "method": "GET",
    "checkIntervalSeconds": 60,
    "timeoutSeconds": 30,
    "environment": "prod",
    "headers": "{\"Authorization\": \"Bearer token\"}",
    "body": null
  }
  ```
- `PUT /api/apis/{id}` - Update API
- `DELETE /api/apis/{id}` - Delete API

### Metrics
- `GET /api/metrics?environment=prod&startTime=2024-01-01T00:00:00Z&endTime=2024-01-01T23:59:59Z` - Get dashboard metrics
- `GET /api/metrics/apis?environment=prod` - Get metrics breakdown by API

### Collections
- `GET /api/collections` - List all collections
- `GET /api/collections/{id}` - Get collection by ID
- `POST /api/collections` - Create a new collection
- `PUT /api/collections/{id}` - Update collection
- `DELETE /api/collections/{id}` - Delete collection
- `POST /api/collections/{collectionId}/apis/{apiId}` - Add API to collection
- `DELETE /api/collections/{collectionId}/apis/{apiId}` - Remove API from collection

### Alerts
- `GET /api/alerts?active=true` - List alerts (optional active filter)
- `GET /api/alerts/{id}` - Get alert by ID
- `POST /api/alerts` - Create a new alert
  ```json
  {
    "name": "High Error Rate",
    "description": "Alert when error rate exceeds 10%",
    "type": 0,
    "condition": 0,
    "threshold": 10.0,
    "apiId": 1,
    "collectionId": null,
    "isActive": true
  }
  ```
  Alert Types: `0` = ErrorRate, `1` = Latency, `2` = Uptime, `3` = StatusCode
  Conditions: `0` = GreaterThan, `1` = LessThan, `2` = Equals, `3` = NotEquals
- `PUT /api/alerts/{id}` - Update alert
- `DELETE /api/alerts/{id}` - Delete alert
- `GET /api/alerts/history?resolved=false&alertId=1` - Get alert history
- `POST /api/alerts/history/{id}/resolve` - Resolve an alert

### Health Checks
- `GET /api/healthchecks?apiId=1&startTime=2024-01-01T00:00:00Z&endTime=2024-01-01T23:59:59Z&page=1&pageSize=100` - Get health check history
- `GET /api/healthchecks/{id}` - Get health check by ID

See Swagger UI at `https://localhost:7000/swagger` for interactive API documentation.

## Project Structure

```
api-monitor/
├── backend/
│   ├── PulseAPI.Api/          # Web API
│   ├── PulseAPI.Core/          # Domain models
│   ├── PulseAPI.Application/   # Business logic
│   ├── PulseAPI.Infrastructure/ # Data access, services
│   └── PulseAPI.Worker/        # Background worker
├── frontend/
│   └── pulseapi-web/           # React app
└── docker-compose.yml          # PostgreSQL setup
```

## Usage Examples

### Creating an API to Monitor

```bash
curl -X POST https://localhost:7000/api/apis \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Example API",
    "url": "https://api.example.com/health",
    "method": "GET",
    "checkIntervalSeconds": 60,
    "timeoutSeconds": 30,
    "environment": "prod"
  }'
```

### Creating an Alert

```bash
curl -X POST https://localhost:7000/api/alerts \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "High Error Rate Alert",
    "description": "Alert when error rate exceeds 5%",
    "type": 0,
    "condition": 0,
    "threshold": 5.0,
    "apiId": 1,
    "isActive": true
  }'
```

### Creating a Collection

```bash
curl -X POST https://localhost:7000/api/collections \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Production APIs",
    "description": "All production APIs"
  }'
```

## Development

### Running Tests

```bash
# Backend tests (when added)
cd backend
dotnet test

# Frontend tests
cd frontend/pulseapi-web
npm test
```

### Building for Production

```bash
# Backend
cd backend
dotnet publish -c Release -o ./publish

# Frontend
cd frontend/pulseapi-web
npm run build
```

The production build will be in `frontend/pulseapi-web/build`.

### Database Migrations

```bash
# Create a new migration
dotnet ef migrations add MigrationName --project PulseAPI.Infrastructure/PulseAPI.Infrastructure.csproj --startup-project PulseAPI.Api/PulseAPI.Api.csproj

# Apply migrations
dotnet ef database update --project PulseAPI.Infrastructure/PulseAPI.Infrastructure.csproj --startup-project PulseAPI.Api/PulseAPI.Api.csproj

# Remove last migration
dotnet ef migrations remove --project PulseAPI.Infrastructure/PulseAPI.Infrastructure.csproj --startup-project PulseAPI.Api/PulseAPI.Api.csproj
```

## Troubleshooting

### Backend Issues

**Database Connection Failed**
- Ensure PostgreSQL is running: `docker-compose ps`
- Check connection string in `appsettings.json`
- Verify database exists: `docker exec -it pulseapi-postgres psql -U postgres -l`

**Migrations Fail**
- Ensure database is created
- Check that EF Core Design package is installed
- Try removing `obj` and `bin` folders and rebuilding

**Worker Service Not Checking APIs**
- Verify worker service is running
- Check logs for errors
- Ensure APIs are marked as `IsActive: true`
- Verify `CheckIntervalSeconds` is set appropriately

### Frontend Issues

**Cannot Connect to API**
- Check `REACT_APP_API_URL` in `.env` file
- Verify backend is running
- Check CORS settings in backend `Program.cs`
- For HTTPS backend, ensure certificate is trusted or use HTTP

**Authentication Not Working**
- Clear browser localStorage
- Verify JWT token is being sent in requests
- Check backend logs for authentication errors

**Metrics Not Updating**
- Ensure worker service is running
- Check that APIs have been checked (see health checks endpoint)
- Verify time range for metrics query

### Common Issues

**Port Already in Use**
- Backend: Change ports in `launchSettings.json`
- Frontend: Set `PORT=3001` in `.env` or use `npm start -- --port 3001`
- PostgreSQL: Change port in `docker-compose.yml`

**CORS Errors**
- Ensure frontend URL is in CORS policy in `Program.cs`
- Check that credentials are allowed if using cookies

## Architecture Details

### Backend Architecture

The backend follows Clean Architecture principles:

- **PulseAPI.Core**: Domain entities and interfaces
- **PulseAPI.Application**: Business logic and DTOs
- **PulseAPI.Infrastructure**: Data access (EF Core), external services (HTTP client), repositories
- **PulseAPI.Api**: Controllers, middleware, API configuration
- **PulseAPI.Worker**: Background service for health checks

### Database Schema

- **Users**: User accounts and authentication
- **Apis**: Monitored API endpoints
- **Collections**: Groups of APIs
- **ApiCollections**: Many-to-many relationship between APIs and Collections
- **HealthChecks**: Historical health check results
- **Alerts**: Alert definitions
- **AlertHistories**: Fired alert records

### Worker Service Flow

1. Every 10 seconds, worker checks all active APIs
2. For each API, checks if `CheckIntervalSeconds` has elapsed since last check
3. Performs HTTP health check using configured method, headers, and body
4. Stores result in `HealthChecks` table
5. Evaluates alerts for the API
6. Evaluates collection-level alerts

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

MIT

## Support

For issues and questions, please open an issue on the GitHub repository.



