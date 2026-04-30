# Aihrly — Hiring Pipeline API

A REST API for managing the recruiter-side hiring pipeline. Built with ASP.NET Core 8, PostgreSQL, and EF Core.

---

## What I Built

A hiring pipeline API that lets recruiters and hiring managers post jobs, receive candidate applications, move candidates through stages, leave notes, and score candidates across three dimensions.

The system tracks every action — who moved a candidate, who left a note, who set a score — because hiring is a team sport and accountability matters.

---

## Tech Stack

- ASP.NET Core 8
- PostgreSQL via EF Core (Npgsql)
- xUnit for testing
- Swagger/OpenAPI for API documentation

---

## How to Run Locally

### Prerequisites

- .NET 8 SDK
- PostgreSQL running locally

### 1. Clone the repo

```bash
git clone <repo-url>
cd Aihrly
```

### 2. Set up the database

Create the database in PostgreSQL:

```bash
psql -h localhost -U postgres -c "CREATE DATABASE aihrly;"
```

### 3. Update the connection string

Open `appsettings.json` and update the connection string to match your PostgreSQL credentials:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Database=aihrly;Username=postgres;Password=yourpassword"
}
```

### 4. Run migrations

```bash
dotnet ef database update
```

This creates all tables and seeds the 3 default team members automatically.

### 5. Start the API

```bash
dotnet run
```

The API will be available at `http://localhost:5287`.

Swagger UI is available at `http://localhost:5287/swagger`.

---

## Seeded Team Members

Three team members are seeded automatically when migrations run. Use their IDs in the `X-Team-Member-Id` header for all mutating requests.

| Name | Role | ID |
|---|---|---|
| Alice Johnson | Recruiter | `11111111-1111-1111-1111-111111111111` |
| Bob Smith | HiringManager | `22222222-2222-2222-2222-222222222222` |
| Carol White | Recruiter | `33333333-3333-3333-3333-333333333333` |

---

## How to Run the Tests

### Prerequisites

Create the test database:

```bash
psql -h localhost -U postgres -c "CREATE DATABASE aihrly_test;"
```

### Run all tests

```bash
dotnet test
```

### What is tested

- **Stage transition rules** — valid and invalid transitions, terminal stage detection
- **Note creation with author name** — creates an application, adds a note, confirms the author name is resolved correctly (not just the ID)
- **Score overwrite** — submits a score twice and confirms the second value wins with correct `updatedAt` timestamp
- **Duplicate application rule** — same email on the same job returns `409 Conflict`

---

## API Endpoints

### Jobs

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/jobs` | Create a job |
| `GET` | `/api/jobs` | List jobs. Filter: `?status=open`. Paginate: `?page=1&pageSize=20` |
| `GET` | `/api/jobs/{id}` | Get a single job |

### Applications

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/jobs/{jobId}/applications` | Candidate applies to a job |
| `GET` | `/api/jobs/{jobId}/applications` | List applications. Filter: `?stage=screening` |
| `GET` | `/api/applications/{id}` | Full candidate profile with scores, notes, and stage history |
| `PATCH` | `/api/applications/{id}/stage` | Move application to a new stage |

### Notes

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/applications/{id}/notes` | Add a note |
| `GET` | `/api/applications/{id}/notes` | List notes, newest first |

### Scores

| Method | Endpoint | Description |
|---|---|---|
| `PUT` | `/api/applications/{id}/scores/culture-fit` | Set culture fit score (1-5) |
| `PUT` | `/api/applications/{id}/scores/interview` | Set interview score (1-5) |
| `PUT` | `/api/applications/{id}/scores/assessment` | Set assessment score (1-5) |

---

## Pipeline Stages and Valid Transitions

```
Applied   → Screening, Rejected
Screening → Interview, Rejected
Interview → Offer,     Rejected
Offer     → Hired,     Rejected
Hired     → (terminal)
Rejected  → (terminal)
```

Invalid transitions return `400` with a clear error message.

---

## Assumptions Made

- New applications always start at the `Applied` stage — this is not configurable per job.
- The `X-Team-Member-Id` header returns `400 Bad Request` for missing or invalid values. `401` was considered but `400` felt more accurate since there is no authentication scheme in place.
- Invalid `status` or `stage` filter values in query parameters are silently ignored and return all results unfiltered.
- Submitting a score for a dimension that already has a value completely overwrites the previous score. The original `SetBy` and `SetAt` are preserved; `UpdatedBy` and `UpdatedAt` reflect the latest change.
- Cover letter is optional on applications.

---

## What I Would Improve With More Time

- **More test coverage** — the current 11 tests cover the core rules but I would add tests for invalid stage transitions via the API, missing headers, and score validation boundaries.
- **Input sanitization** — currently relying on EF Core parameterized queries to prevent SQL injection, but I would add explicit sanitization on free-text fields like notes and cover letters.
- **Redis caching** — cache `GET /api/applications/{id}` with a short TTL and invalidate on any write. This endpoint is called most frequently by the pipeline UI.
- **Soft deletes** — instead of hard deletes, mark records as deleted so history is preserved.
- **Pagination on notes and stage history** — currently returns all records. For active pipelines with many candidates, this could become a performance issue.

---

## Part 2 — Background Job (Option A)

I chose Option A — background notifications when an application moves to `Hired` or `Rejected`.

Due to time constraints this was not fully implemented. My approach would have been:

- Use ASP.NET Core's built-in `BackgroundService` or `IHostedService` no external libraries needed
- When `PATCH /api/applications/{id}/stage` moves a candidate to `Hired` or `Rejected`, enqueue a notification task and return immediately the endpoint never waits for the notification
- A background worker picks up the task, writes a log line, and inserts a row into a `notifications` table with `id`, `application_id`, `type`, and `sent_at`
- This keeps the PATCH endpoint fast and decoupled from the notification logic

Given more time I would implement this using a `Channel<T>` as an in-memory queue — lightweight, no Redis or external broker needed for this scale.

---

## Approximate Hours Spent

7 hours.
