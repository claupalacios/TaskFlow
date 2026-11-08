# TaskFlow

A small task manager: users register, log in, and manage their own private tasks (title, description, status, due date).

> Work in progress: the API is complete; the Angular frontend and design notes come next.

## Stack
- ASP.NET Core Web API on .NET 10, Clean Architecture, xUnit
- Angular 21 (planned)

## Structure
```
src/
  TaskFlow.Domain          entities and invariants (no dependencies)
  TaskFlow.Application     use cases, business rules, ports
  TaskFlow.Infrastructure  EF Core (SQLite), hashing, JWT
  TaskFlow.Api             controllers, composition root
tests/
  TaskFlow.Application.Tests
  TaskFlow.Infrastructure.Tests
  TaskFlow.Api.Tests
```

## Build and test
Requires the .NET 10 SDK (pinned in `global.json`).
```bash
dotnet build
dotnet test
```

## Run the API locally
1. Create a JWT signing key. It is kept in [user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), outside the repository. Run this once from the repository root.

   bash:
   ```bash
   dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project src/TaskFlow.Api
   ```
   PowerShell:
   ```powershell
   $b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b); dotnet user-secrets set "Jwt:SigningKey" ([Convert]::ToBase64String($b)) --project src/TaskFlow.Api
   ```
2. Start the API. In Development it applies migrations to a local `taskflow.db` and seeds the demo data.
   ```bash
   dotnet run --project src/TaskFlow.Api --launch-profile http
   ```
3. Open Swagger UI at http://localhost:5279/swagger. Pick the **Tasks** or **Identity** API from the selector. Log in with `POST /api/auth/login`, then paste the `accessToken` into **Authorize**. You can also use [`TaskFlow.Api.http`](src/TaskFlow.Api/TaskFlow.Api.http).

**Demo accounts**

| Email | Password |
|---|---|
| `ana@example.com` | `TaskFlow-demo1` |
| `bruno@example.com` | `TaskFlow-demo2` |

Each account sees only its own tasks.

## Other environments
Automatic migrations and seeding are enabled only in `appsettings.Development.json`. Anywhere else:
- Provide the key with the `Jwt__SigningKey` environment variable. It must be at least 32 bytes, or the API refuses to start.
- Apply migrations as an explicit step before starting the API:
  ```bash
  dotnet tool restore
  dotnet ef database update --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.Api --connection "<connection string>"
  ```
