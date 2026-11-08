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

## How AI output was validated

- Every red phase was committed on its own and run to confirm the tests compiled and failed for missing behavior, before any implementation.
- `dotnet build` (warnings as errors) and `dotnet test` ran after each green phase and again before each merge.
- I reviewed the diff of every phase before approving it, and each merge into `main` waited for that review.
- For the date converter, the fix was turned off to confirm the test then fails, so the tests catch real errors and do not just pass.
