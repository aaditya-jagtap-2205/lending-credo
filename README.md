# Lending Platform

A small secured-lending platform: an ASP.NET Core Web API that scores loan applications against a
credit policy, persists them to SQLite, and a React frontend for submitting applications and
reviewing the book.

## Running it

You need the .NET 10 SDK and Node.js 18+.

### 1. API

```bash
cd backend/LendingPlatform.Api
dotnet run
```

The API starts on `http://localhost:5199`. The SQLite file (`lending.db`) is created automatically
on first run. Swagger UI is at **http://localhost:5199/swagger** and `/` redirects there.

### 2. Frontend

In a second terminal:

```bash
cd frontend
npm install
npm run dev
```

Open **http://localhost:5173**. The frontend expects the API on `http://localhost:5199`; to point it
elsewhere, copy `.env.example` to `.env` and set `VITE_API_BASE_URL`.

### 3. Tests

```bash
cd backend
dotnet test
```

## API

| Method | Route | Purpose |
| --- | --- | --- |
| POST | `/api/loans` | Submit an application, get a decision, store the result |
| GET | `/api/loans` | Application history, newest first |
| GET | `/api/loans/statistics` | Applicant counts, total lent, mean LTV |

Example:

```bash
curl -X POST http://localhost:5199/api/loans \
  -H "Content-Type: application/json" \
  -d '{"loanAmount":750000,"assetValue":1000000,"creditScore":820}'
```

## The rules

`LTV = (loan amount / asset value) × 100`

- Decline if the loan is below £100,000 or above £1,500,000.
- Loan of £1,000,000 or more: LTV must be 60% or less **and** credit score at least 950.
- Loan below £1,000,000: LTV under 60% needs 750; under 80% needs 800; under 90% needs 900;
  90% or above is declined.

## Structure

```
backend/LendingPlatform.Api/
  Controllers/LoansController.cs      HTTP only - binding, status codes, delegation
  Services/LoanDecisionService.cs     the lending rules; no EF, no HTTP, no I/O
  Services/StatisticsCalculator.cs    the reporting aggregations, pure
  Services/LoanApplicationService.cs  use case: evaluate, persist, read back
  Data/LendingDbContext.cs            EF Core mapping
  Models/                             entities
  Dtos/                               request/response shapes used by the API
backend/LendingPlatform.Tests/        xUnit tests for the rules and the statistics
frontend/src/                         React (Vite) UI
```

Request flow: React → `LoansController` → `LoanApplicationService` → `LoanDecisionService` for the
decision and EF Core → SQLite for persistence. No rule lives in the controller or the frontend.
Entities are never returned directly; everything crosses the API boundary as a DTO.

## Assumptions

These were the genuinely ambiguous points in the brief. Each is a defensible reading and each is
easy to flip in one place if the business disagrees.

1. **Mean LTV covers all applications, including declined ones.** The brief says "across all
   applications", so declines are included. If it should be successful applications only, the change
   is one filter in `StatisticsCalculator`.
2. **Total value of loans written counts successful applications only.** A decline results in no
   money being lent, so it cannot be part of the value written.
3. **The band boundaries are read literally.** "LTV must be 60% or less" makes exactly 60% acceptable
   for large loans. For loans under £1m, "if LTV < 60%" means exactly 60.00% is *not* in that band and
   falls through to the "< 80%" band, requiring a score of 800. `£100,000` and `£1,500,000` are both
   accepted, because the rule declines below and above those figures. `£1,000,000` exactly is a large
   loan, because the rule reads "£1 million or more".
4. **Decisions use the unrounded LTV.** The stored and displayed LTV is rounded to 2dp, but the
   comparison against 60/80/90 uses full `decimal` precision, so an application at 89.9999% is judged
   in the 80–90 band rather than being rounded up to a decline.
5. **Credit score is an integer from 1 to 999**, as stated in the inputs; anything outside that is a
   validation error rather than a decline, because it isn't a real application.
6. **Invalid input is a 400, not a decline.** A zero asset value, a negative amount or an
   out-of-range score means the application cannot be assessed at all. Reporting them as "Declined"
   would pollute the statistics with applications that were never underwritten.
7. **No applicant identity is modelled.** The brief asks for the number of applicants grouped by
   status, and there is no applicant entity in the inputs, so one application is treated as one
   applicant.

## Trade-offs and what a production version would change

- **`EnsureCreated()` instead of EF migrations.** Fine for a reviewer who wants `dotnet run` to work
  first time; production would use migrations applied as a deployment step.
- **Statistics are calculated in memory.** SQLite has no native decimal type, so EF stores decimals
  as text and cannot `SUM`/`AVG` them in SQL. At assessment scale, loading the rows and aggregating
  in C# is correct and exact. At real volume this becomes a maintained aggregate table or a
  database that supports `NUMERIC` properly.
- **The policy is compiled in as constants.** Real credit policy changes without a redeploy, so it
  would move to configuration or a versioned policy record, and each application would store the
  policy version that judged it, for audit.
- **No authentication, no rate limiting, no audit trail of who submitted what.** Out of scope here,
  essential in production.
- **Decision reasons are English strings.** Good enough to explain an outcome in the UI; production
  would use a reason code with a presentation-layer lookup.
- **No integration tests over the HTTP layer.** The rules and aggregations — where the risk is — are
  covered by unit tests. A production suite would add `WebApplicationFactory` tests for validation
  responses and persistence.
