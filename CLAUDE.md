# TaskFlow — notes for Claude Code

Task management app for the Ballast Lane .NET assessment: ASP.NET Core Web API (.NET 10) + Angular 21, Clean Architecture, TDD.

## Commands
- Build: `dotnet build` (warnings are errors)
- Test: `dotnet test`

## Architecture rules
- `Domain` references nothing. `Application` references only `Domain`.
- `Infrastructure` implements Application interfaces (EF Core, hashing, JWT).
- `Api` is the composition root; it is the only project that references `Infrastructure`.
- No EF Core or ASP.NET types in `Domain` or `Application`.
- Tasks are always scoped to the authenticated user; the user id comes from the token, never the request body. Another user's task returns 404.

## Working agreement
- Work one milestone at a time on its own branch; merge into `main` with `--no-ff` only when green.
- Core rules are built test-first: a compiling red `test:` commit, then a green `feat:` commit.
- Every commit must build. Conventional Commits.
- Do not add packages, projects, or patterns beyond the approved plan without asking.
- Never commit the assessment PDF, `TaskFlow_Roadmap.md`, secrets, or local `*.db` files.
