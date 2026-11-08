# Generative AI usage

I built TaskFlow with Claude Code, one milestone at a time, each on its own branch. Core rules were built test-first: Claude wrote the failing tests, I reviewed them before any implementation, and each milestone was merged into `main` only after my approval and a green build. Commits written with AI carry a `Co-Authored-By: Claude` trailer.

## Initial project prompt (representative)

```text
I'm doing a technical assessment: a task management app with an ASP.NET Core
Web API on .NET 10 and an Angular frontend. Users register, log in, and do CRUD
on their own tasks. I want Clean Architecture and TDD, kept simple, nothing I
couldn't defend in an interview.

Let's work in small milestones, each on its own branch. For every milestone:
plan it first, write failing tests, stop so I can review them, then implement.
Only merge into main after I approve. Don't write any code yet; start with a plan
I can review.
```

## Milestones

Prompts are summarized and reworded for readability. Under each milestone, items I raised are listed first; items Claude found on its own are marked **Found by Claude**.

### M1 — Task domain

**Prompt summary**
Start with the task domain: a `TaskItem` entity with its validation rules, and a `TaskService` on top of a repository interface. Failing tests first, then stop for my review.

**AI contribution**
- Wrote the red test suites for `TaskItem` and `TaskService`, then the implementations.

**My review and corrections**
- The `TaskItem` tests had gaps. I added cases for title length and trimming on update, and required that a failed update leave the task unchanged.
- Claude rejected past due dates on create but allowed them on update. I found that inconsistent and not required by the PDF, and changed the rule to allow them on both.
- Every test passed `CancellationToken.None`, so an implementation that ignored the token would still pass. I asked for a test proving a real token reaches the repository.

**Result:** merged after both red phases were reviewed and the green phases passed.

### M2 — Persistence

**Prompt summary**
Persistence with EF Core and SQLite. Same TDD flow: repository tests against in-memory SQLite, the red phase in its own commit, then wait for me.

**AI contribution**
- Wrote the repository tests, the EF Core repositories and the initial migration.

**My review and corrections**
- I set the test scope: user isolation, integrity constraints and date persistence, with the red phase committed on its own.
- **Found by Claude:** SQLite cannot order by `DateTimeOffset`. Dates are stored as UTC ticks, and turning the converter off makes the ordering test fail.

**Result:** merged.

### M3 — Authentication core

**Prompt summary**
The auth core. Define the password rules explicitly and cover registration, duplicate emails, invalid login and the security cases. Red first, no implementation without my approval.

**AI contribution**
- Wrote the auth tests, the password policy, `AuthService`, the password hasher, the JWT token service and the demo seeder.

**My review and corrections**
- I required explicit password rules and security cases in the red phase, not just the happy path.
- **Found by Claude:** two concurrent registrations could bypass the duplicate-email check and end in a 500. The repository now reports it as a conflict (409).
- **Found by Claude:** in self-review, a test password meant to break the policy actually satisfied it. It was replaced before the commit.

**Result:** merged.

### M4 — API

**Prompt summary**
The API. Tests first for the endpoints, authentication and error handling, and no implementation until we review the failing tests.

**AI contribution**
- Wrote the endpoint and token tests, the controllers, the Problem Details error handler, JWT wiring and Swagger UI.

**My review and corrections**
- I rejected committing a development JWT key in `appsettings.Development.json`, since the repository is public. The key comes from `dotnet user-secrets`, and startup fails without it.
- I limited automatic migrations to Development; anywhere else they are an explicit step.
- I added a case before implementation: a correctly signed token with no valid `sub`. It returned a 500; it is now rejected with a 401 during token validation.
- Before the merge, I asked for a clean tree, a clean `git diff --check`, and proof that no secrets or local databases were committed.
- **Found by Claude:** the endpoint tests caught a `[Produces("application/json")]` attribute that broke `application/problem+json` responses, and it was removed.

**Result:** merged.

## How AI output was validated

- Every red phase was committed on its own and run to confirm the tests compiled and failed for missing behavior, before any implementation.
- `dotnet build` (warnings as errors) and `dotnet test` ran after each green phase and again before each merge.
- I reviewed the diff of every phase before approving it, and each merge into `main` waited for that review.
- Security behavior has endpoint tests: requests without a valid token return 401, another user's task returns 404, and a duplicate email returns 409.
- For the date converter and the `sub` check, the fix was turned off to confirm the test then fails, so the tests catch real errors and do not just pass.
