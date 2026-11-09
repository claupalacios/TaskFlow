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

### M5a — Angular scaffold

**Prompt summary**
Plan the Angular frontend first, then scaffold the workspace.

**AI contribution**
- Proposed the frontend plan and scaffolded the workspace, app shell and API proxy.

**My review and corrections**
- I approved the proposed stack (Angular 21, Angular Material, Signals, Reactive Forms, Vitest), added ESLint and kept Playwright optional.
- I asked the plan to handle per-field validation errors, avoid redirect loops on 401 and clear task state on logout. These are scheduled for M5b.
- I required `--skip-git`, so `ng new` would not create a nested Git repository.
- I pushed back on the five-hour estimate and asked for a minimum viable scope.
- **Found by Claude:** `ng new` generated `.vscode/mcp.json` despite `--ai-config=none`. It was removed before the first commit.

**Result:** merged.

### M5b — Frontend authentication

**Prompt summary**
Sign-in, registration and route protection in the Angular app, test-first, then a check against the running API before the merge.

**AI contribution**
- Wrote the specs and the implementation: session handling, token and 401 interceptors, guards, return URL checks, problem details parsing, the login and register pages and the routes.
- Ran the auth flows in a browser against the local API.

**My review and corrections**
- The plan items I added in M5a are covered: server field errors appear under the matching field, and return URLs that point to the login or register page are rejected so sign-in cannot loop. Clearing task state on logout moves to M6, when the app has task state.
- I approved three files beyond the plan: a helper for server field errors, a shared stylesheet for the auth pages, and a placeholder `/tasks` page until M6.
- I required a check against the running API before the merge, not only unit tests.
- **Found by Claude:** that check showed the Log out button with dark text on the blue toolbar. The toolbar now sets its colors through Material tokens, so its buttons use the light text color.

**Result:** merged.

### M6 — Task screens

**Prompt summary**
The task screens in Angular. Plan first and wait for approval, then two red/green cycles (data layer, then UI), then a manual check against the running API before the merge.

**AI contribution**
- Proposed the plan, then wrote the specs and the implementation: the Tasks API client, the task store, date helpers, the task list with a status filter, one form dialog for create and edit, the delete confirmation and the quick status menu.
- Ran the manual checks in a browser against the local API and database.

**My review and corrections**
- I pointed out that providers registered on a route are not destroyed when the user leaves it, so the proposed route-level store could keep one user's tasks after logout. The store is now provided by the tasks page, its dialogs use that same instance, and a test checks that a new instance is created on return.
- I pointed out that `closeOnNavigation` reacts to history changes, not to router navigation such as logout. The page now closes the dialogs it opened when it is destroyed, with a test.
- I approved the quick status change on condition that it sends the full task, so title, description and due date never change. Tests cover it, and the manual check confirmed it in the database.
- I chose one form for create and edit, asked for the README update in this milestone, and set the priorities: working CRUD, session isolation, validation and errors, tests, API integration, visuals.
- Before the merge, I required a manual check of CRUD, persistence after reload, switching from Ana to Bruno with no Ana data shown even while loading, and no operation after leaving the page with a dialog open.
- **Found by Claude:** the first green build was over the 500 kB bundle budget (846 kB). Pages are now lazy-loaded, and the initial bundle is 399 kB.
- **Found by Claude:** two specs assumed wrong timing: a delete starts only after the confirmation closes, and lazy pages compiling inside the first route test went over the 5-second test timeout. The specs now wait for the dialog to close and preload the pages; no assertion changed.

**Result:** merged.

### M7 — Quality, CI and delivery

**Prompt summary**
The last milestone: a security audit before publishing, CI for .NET and Angular, a documentation review, a check against the assessment and the public repository. Plan first; review the results before every push and merge.

**AI contribution**
- Audited the tree and the git history for secrets, local databases and private files. Also audited the dependencies and the JWT configuration.
- Wrote the GitHub Actions workflow and ran its steps locally, including the frontend in a fresh clone.
- Added the user story to the README and the representative code samples to this file. Checked the app in a browser for console warnings.

**My review and corrections**
- I kept the original commits and their author email instead of rewriting the history.
- I asked for a dependency audit without automatic updates.
- I kept the OpenAPI documents available in every environment and asked to document it.
- I allowed `dotnet format` in CI only if it needed no mass reformatting. It needed none, so it is a CI step.
- I made the representative code samples a priority, since the PDF asks for them.
- I required that the delivery link point to `main`.
- **Found by Claude:** this file had no samples of generated code, although the PDF asks for them. The section below was added.
- **Found by Claude:** `npm audit` reports 31 moderate issues, all in development tooling (`minimatch` and `glob`, through the Angular CLI and ESLint). Production dependencies and NuGet packages have none, so nothing was updated.

**Result:** CI and documentation ready on the branch; merged after CI passes on GitHub and my review.

## Representative generated code

Excerpts of code Claude generated, as they are in the repository after my review. Each one shows a decision or correction described above.

**Domain rule** ([`TaskItem.cs`](../src/TaskFlow.Domain/TaskItem.cs)). From M1: a failed update must leave the task unchanged, so every value is validated before anything is assigned.

```csharp
public void Update(string? title, string? description, TaskItemStatus status, DateOnly? dueDate, DateTimeOffset now)
{
    // Validate everything first so a failed update never leaves the task half-changed.
    var newTitle = NormalizeTitle(title);
    var newDescription = NormalizeDescription(description);

    if (!Enum.IsDefined(status))
    {
        throw new DomainValidationException($"'{status}' is not a valid task status.");
    }

    Title = newTitle;
    Description = newDescription;
    Status = status;
    DueDate = dueDate;
    UpdatedAt = now;
}
```

**Use case** ([`TaskService.cs`](../src/TaskFlow.Application/Tasks/TaskService.cs)). Every operation receives the caller's id, and the repository only returns that user's tasks, so another user's task is a 404.

```csharp
public async Task<TaskResponse> UpdateAsync(Guid userId, Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
{
    var task = await GetOwnedTaskAsync(userId, taskId, cancellationToken);

    task.Update(request.Title, request.Description, request.Status, request.DueDate, timeProvider.GetUtcNow());

    await repository.UpdateAsync(task, cancellationToken);

    return ToResponse(task);
}

// The repository only returns the caller's own tasks, so "someone else's task" and
// "no such task" are indistinguishable here by design.
private async Task<TaskItem> GetOwnedTaskAsync(Guid userId, Guid taskId, CancellationToken cancellationToken)
    => await repository.GetByIdAsync(userId, taskId, cancellationToken)
       ?? throw new NotFoundException($"Task '{taskId}' was not found.");
```

**Endpoint** ([`TasksController.cs`](../src/TaskFlow.Api/Controllers/TasksController.cs)). The user id comes from the token, never from the request body.

```csharp
[HttpPost]
[ProducesResponseType(StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<ActionResult<TaskResponse>> Create(CreateTaskRequest request, CancellationToken cancellationToken)
{
    var task = await tasks.CreateAsync(User.GetUserId(), request, cancellationToken);

    return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
}
```

**Token validation** ([`JwtAuthenticationExtensions.cs`](../src/TaskFlow.Api/Auth/JwtAuthenticationExtensions.cs)). From M4: a correctly signed token without a valid `sub` used to return a 500. It is now rejected with a 401.

```csharp
OnTokenValidated = context =>
{
    if (context.Principal is null || !context.Principal.TryGetUserId(out _))
    {
        context.Fail("The token has no valid subject.");
    }

    return Task.CompletedTask;
},
```

**Endpoint test** ([`TasksEndpointsTests.cs`](../tests/TaskFlow.Api.Tests/Endpoints/TasksEndpointsTests.cs)). It checks user isolation through real HTTP, using the API's real authentication and SQLite.

```csharp
[Fact]
public async Task AnotherUsersTask_GetUpdateAndDeleteReturn404AndLeaveItUntouched()
{
    var owner = await ApiClient.CreateUserClientAsync(factory);
    var intruder = await ApiClient.CreateUserClientAsync(factory);
    var task = await CreateTaskAsync(owner);
    var url = $"/api/tasks/{task.Id}";

    await AssertProblemAsync(await intruder.GetAsync(url), HttpStatusCode.NotFound);
    await AssertProblemAsync(
        await intruder.PutAsJsonAsync(url, new { title = "Hijacked", status = "Completed" }, ApiClient.Json),
        HttpStatusCode.NotFound);
    await AssertProblemAsync(await intruder.DeleteAsync(url), HttpStatusCode.NotFound);

    var stillThere = await owner.GetFromJsonAsync<TaskResponse>(url, ApiClient.Json);
    Assert.Equal(task, stillThere);
}
```

## Edge cases, authentication and validation

- **Validation** lives in the domain and in the Application layer, not in the controllers. Errors are returned as Problem Details:
  - The title is required and trimmed, with a maximum of 200 characters.
  - The description has a maximum of 2000 characters.
  - An unknown status returns 400.
  - Due dates in the past are allowed (my decision in M1).
- **Passwords** must have 8–128 characters, with at least one letter and one digit, and must not equal the email. They are hashed with ASP.NET Core Identity's `PasswordHasher`.
- **Authentication** uses HS256 JWTs with issuer, audience, lifetime and algorithm checks. The signing key must have at least 32 bytes, or the API refuses to start, and it never lives in the repository.
- **Login:** a wrong password and an unknown email both return 401. Both paths run the same password hash check, so the response time does not reveal which emails exist.
- **Duplicate emails** return 409, including two registrations that arrive at the same time (M3).
- **Authorization:**
  - A request without a valid token returns 401.
  - Another user's task returns 404 for read, update and delete.
  - A `userId` sent in the request body is ignored.
  - A non-GUID id returns 404.

## How AI output was validated

- Every red phase was committed on its own and run to confirm the tests compiled and failed for missing behavior, before any implementation.
- `dotnet build` (warnings as errors) and `dotnet test` ran after each green phase and again before each merge.
- I reviewed the diff of every phase before approving it, and each merge into `main` waited for that review.
- Security behavior has endpoint tests: requests without a valid token return 401, another user's task returns 404, and a duplicate email returns 409.
- Frontend flows were also run in a browser against the local API before the merge.
- For the date converter, the `sub` check, request cancellation in the task store and the task page's isolation (page-level store, closing dialogs), the fix was turned off to confirm the tests then fail, so the tests catch real errors and do not just pass.
