# TaskFlow

A small task manager: users register, log in, and manage their own private tasks (title, description, status, due date).

> Work in progress. Setup instructions, demo credentials, and design notes will be added as milestones land.

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
