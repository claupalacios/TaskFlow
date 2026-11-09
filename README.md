# TaskFlow

A small task manager: users register, log in, and manage their own private tasks (title, description, status, due date).

## User story
> I often have several things to get done and need a simple way to keep track of them.
>
> I'd like to create tasks, set deadlines, update their progress, and remove them when they're no longer needed.
>
> Since other people might use the application too, I want my tasks to remain private.

How AI was used to build it, with representative generated code and what was corrected, is in [docs/GENAI.md](docs/GENAI.md).

## Stack
- ASP.NET Core Web API on .NET 10, Clean Architecture, xUnit
- Angular 21 with Angular Material, Signals and Reactive Forms; Vitest for unit tests

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
web/taskflow-web           Angular app (auth pages, task list and dialogs)
```

## Build and test
Requires the .NET 10 SDK (pinned in `global.json`) and a Node.js version supported by Angular 21 (20.19+, 22.12+ or 24).
```bash
dotnet build
dotnet test
```
```bash
cd web/taskflow-web
npm ci
npm test -- --watch=false
npm run lint
```
GitHub Actions runs the same checks on every push ([`.github/workflows/ci.yml`](.github/workflows/ci.yml)).

## Run locally
Start the API first, then the web app.

### API
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

### Web app
In a second terminal:
```bash
cd web/taskflow-web
npm ci
npm start
```
Open http://localhost:4200 and sign in with a demo account, or create one. The dev server forwards `/api` to the API on port 5279 (`proxy.conf.json`), so the API must be running.

## Other environments
Automatic migrations and seeding are enabled only in `appsettings.Development.json`. Swagger UI is also Development-only, while the OpenAPI documents (`/openapi/tasks.json`, `/openapi/identity.json`) are served in every environment; they describe the endpoints and contain no secrets. Anywhere else:
- Provide the key with the `Jwt__SigningKey` environment variable. It must be at least 32 bytes, or the API refuses to start.
- Apply migrations as an explicit step before starting the API:
  ```bash
  dotnet tool restore
  dotnet ef database update --project src/TaskFlow.Infrastructure --startup-project src/TaskFlow.Api --connection "<connection string>"
  ```
