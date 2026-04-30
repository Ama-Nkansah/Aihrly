# ANSWERS.md

---

## Question 1 — Schema Design

### Tables and their purpose

**`applications`**
- `id` (PK), `job_id` (FK), `candidate_name`, `candidate_email`, `cover_letter`, `stage`, `applied_at`
- Index on `job_id` — most queries filter applications by job
- Unique index on `(job_id, candidate_email)` — enforces no duplicate applications

**`application_notes`**
- `id` (PK), `application_id` (FK), `type`, `description`, `created_by_id` (FK), `created_at`
- Index on `application_id` — notes are always fetched by application

**`stage_history`**
- `id` (PK), `application_id` (FK), `from_stage`, `to_stage`, `changed_by_id` (FK), `changed_at`, `reason`
- Index on `application_id` — history is always fetched by application

**`application_scores`**
- `id` (PK), `application_id` (FK), `dimension`, `score`, `comment`, `set_by_id` (FK), `set_at`, `updated_by_id` (FK, nullable), `updated_at` (nullable)
- Unique index on `(application_id, dimension)` — one score per dimension per application
- Index on `application_id` — scores are always fetched by application

---

### The query for `GET /api/applications/{id}`

```sql
SELECT a.*, j.title,
       n.*, nm.name AS author_name,
       h.*, hm.name AS changed_by_name,
       s.*, sm.name AS set_by_name, um.name AS updated_by_name
FROM applications a
JOIN jobs j ON j.id = a.job_id
LEFT JOIN application_notes n ON n.application_id = a.id
LEFT JOIN team_members nm ON nm.id = n.created_by_id
LEFT JOIN stage_history h ON h.application_id = a.id
LEFT JOIN team_members hm ON hm.id = h.changed_by_id
LEFT JOIN application_scores s ON s.application_id = a.id
LEFT JOIN team_members sm ON sm.id = s.set_by_id
LEFT JOIN team_members um ON um.id = s.updated_by_id
WHERE a.id = $1
```

**How many round-trips:** One. EF Core uses eager loading (`Include` / `ThenInclude`) to load all related data in a single query. The frontend gets everything — basic info, scores, notes with author names, full stage history — in one HTTP request.

---

## Question 2 — Scoring Design Trade-off

### 2a — Why three separate endpoints vs one generic endpoint?

**Three separate endpoints are better when:**
- Each dimension has different validation rules or business logic in the future (e.g. interview score requires a note)
- The frontend saves scores independently — a recruiter fills in culture fit immediately after a call, interview score days later
- You want clear, explicit API contracts — `PUT /scores/culture-fit` is self-documenting
- Access control per dimension becomes a requirement — only interviewers can set the interview score

**One generic endpoint is better when:**
- All three scores are always submitted together at the same time
- You want to reduce the number of API calls — one request instead of three
- The validation rules are identical across all dimensions

For this use case, separate endpoints make more sense. Scores are set at different points in the hiring process by different people.

### 2b — If we needed score history

Currently the schema stores only the current score per dimension. To track history I would:

- Add a new `application_score_history` table:
  - `id`, `application_score_id` (FK), `score`, `comment`, `set_by_id`, `recorded_at`
- Every time a score is updated, insert the old value into `application_score_history` before overwriting
- The `application_scores` table stays as-is — it still holds the current value for fast reads

The endpoints would not change. The response could optionally include a `history` array on the score object. This is a non-breaking addition.

---

## Question 3 — Debugging: Candidate Stuck in Screening

A recruiter reports they moved a candidate to Interview yesterday but today the system shows Screening.

**How I would investigate:**

- **Check the stage_history table** — query `SELECT * FROM stage_history WHERE application_id = $1 ORDER BY changed_at DESC`. If a row exists for the move, the backend received and saved it.

- **Check if the row exists but the application still shows Screening** — this would point to a bug where the stage on the `applications` table was not updated even though history was written. In our code, both happen in the same `SaveChangesAsync()` call so this should not be possible, but I would verify.

- **Check the API logs for the PATCH request** — look for the request timestamp, the team member ID used, and whether a `200` response was returned. If no log entry exists, the request never reached the server.

- **Check the browser network tab** — ask the recruiter to reproduce the move and inspect the PATCH request in DevTools. Check the request payload (`targetStage` value), the response code, and whether the `X-Team-Member-Id` header was sent correctly.

- **Check for a race condition** — if two people were editing the same application simultaneously, one write could have overwritten the other. The stage_history timestamps would reveal this.

- **Check the client-side state** — the frontend might be showing cached/stale data. Ask the recruiter to hard refresh and check again before assuming it is a backend issue.

- **Check for a failed transaction** — if `SaveChangesAsync()` threw an exception after the history was written, a partial write could leave the data inconsistent. Our implementation writes both in the same transaction so this is unlikely, but worth checking the error logs.

- **Reproduce it in a test environment** — attempt the same stage move via Swagger with the same application ID and team member ID to confirm whether it is reproducible.

---

## Question 4 — Honest Self-Assessment

| Skill | Rating | Comment |
|---|---|---|
| C# | 2/5 | I am coming from a JavaScript and Python background. I understand the concepts but I am still getting used to the syntax, type system, and conventions. |
| SQL | 3/5 | Comfortable with queries, joins, and indexes. Less experienced with advanced PostgreSQL-specific features. |
| Git | 4/5 | Comfortable with commits, branching, and pull requests in daily use. |
| REST API Design | 3/5 | I understand resource naming, HTTP methods, and status codes. Still building intuition for edge cases and versioning. |
| Writing Tests | 2/5 | I understand what to test and why, but setting up integration test infrastructure in C# was the hardest part of this assessment for me. I would improve with more practice. |
