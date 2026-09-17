# AI log

Tool used: **Claude (Anthropic)** for architecture discussion, edge-case generation, code review and
documentation. I used it as a pair programmer: it drafted, I specified, questioned and verified. Every
decision below was checked against the brief before it went into the repo.

> Note to me before submitting: replace anything here that doesn't match what I actually did, and add
> any prompts from my own session. The log should be my record, not a template.

---

## 1. Clarifying the brief before writing code

**Prompt.** "Here is the lending brief. Before any code, list every requirement that is ambiguous or
where two reasonable engineers would implement different behaviour, and tell me what the options are."

**What came back (the useful parts).** Six ambiguities, four of which mattered:

- Does "mean LTV across all applications" include declined applications?
- Does "total value of loans written" include declined applications?
- Is an LTV of exactly 60% inside the "< 60%" band or the "< 80%" band?
- Is a £1,000,000 loan "≥ £1 million" (large-loan rules) or under the threshold?

**My decision.** I read each literally and documented it in the README's Assumptions section rather
than silently picking one. The one I spent the longest on was LTV exactly 60%: the large-loan rule
says "60% or less" (inclusive), while the small-loan ladder says "< 60%" (exclusive). Those are
genuinely different comparisons in the same brief, so I implemented them differently and wrote a test
for each — `LargeLoan_...` expects 60% to pass, `SmallLoan_...` expects 60% to require a score of 800.
I would confirm this with the business in a real project.

## 2. Architecture

**Prompt.** "Propose a structure for this that keeps the lending rules independently testable, with no
rule logic in the controller or the frontend. Keep it small — this is a 3-day assessment, not a
platform. Tell me what you'd deliberately leave out."

**Outcome.** Controller → application service → rules engine, with the rules engine a pure class that
takes three primitives and returns a decision. The first draft it offered had the rules engine taking
the EF entity and writing to the `DbContext` itself. I rejected that: it would have made the rules
untestable without a database, which is the exact thing the brief is grading. Splitting
`LoanDecisionService` (pure rules) from `LoanApplicationService` (persistence and orchestration) came
out of that pushback, and `StatisticsCalculator` was split out for the same reason.

I also asked it what to leave out and then held to that list: no repository layer over EF, no
MediatR/CQRS, no AutoMapper, no Docker, no auth. EF Core already is the repository and unit of work.

## 3. A real bug found by reviewing AI output: rounding before deciding

The first generated engine did this:

```csharp
var ltv = Math.Round(loanAmount / assetValue * 100, 2);
if (ltv >= 90) return Declined;
```

I asked: "what happens at a loan of £899,999 against an asset of £1,000,000?"

True LTV is 89.9999%, which is under 90 and should be assessed in the 80–90 band. Rounded to 2dp it
becomes 90.00 and is **declined**. That's a real financial difference caused by a display concern
leaking into a business decision.

**Fix.** The engine decides on the unrounded `decimal` and rounds only the value it returns for
display and storage. `Decision_UsesUnroundedLtv` is the regression test, and it fails against the
original code.

## 4. Independent verification of the rules

I didn't want the tests and the implementation to share a single misreading of the brief, so I wrote a
second implementation of the rules straight from the brief's wording, in a throwaway script, and ran
both across every combination of 15 boundary-relevant loan amounts, 6 asset values and 11 credit
scores (990 cases). Zero mismatches. That's a cross-check, not a substitute for the xUnit suite — it
caught nothing, which was the point.

## 5. Correction: floating-point arithmetic in my own test data

My first version of the boundary theories derived the asset value from a target LTV:

```csharp
var assetValue = loan / ltv * 100m;   // then assert on the recomputed LTV
```

Reviewing this, the round-trip can land a fraction below the target — a "90%" case could evaluate as
89.999…% and pass when it should decline, meaning the test would assert the wrong thing while still
going green. I rewrote both theories to fix the asset value at £1,000,000 / £2,000,000 and vary the
loan amount, so every LTV in the test data is exact and the boundary being tested is unambiguous.

## 6. Correction: statistics and SQLite

The generated statistics query used `db.LoanApplications.AverageAsync(a => a.Ltv)`. SQLite has no
decimal type — EF Core maps `decimal` to TEXT — so server-side `SUM`/`AVG` on these columns is either
unsupported or silently lossy. I changed it to materialise the rows and aggregate in C# with exact
decimal arithmetic, and documented the scaling limitation in the README rather than pretending it
isn't one.

## 7. Correction: invalid input is not a decline

An early draft returned `Declined` for a zero asset value. I disagreed: an application that can't have
its LTV calculated hasn't been underwritten, and recording it as a decline would corrupt both the
decline count and the mean LTV. Invalid input is now a 400 with a field-level message; the engine
throws if called directly with impossible values, as a safety net for non-HTTP callers.

## 8. Frontend

**Prompt.** "Design the UI around what an underwriter actually needs to see. Don't give me a generic
dashboard of four identical cards."

The interesting output was the LTV ruler: a single bar showing where this application's LTV sits
against the 60/80/90 thresholds, which makes the reason for the decision visible rather than just
stated. I kept that and deliberately kept everything else plain.

I asked for client-side validation but was explicit that the backend stays authoritative — the
frontend validation exists so the user gets instant feedback, and the API re-validates everything
regardless. The brief's evaluation criteria would be undermined by rules living in React, so the
frontend never computes a decision; the LTV preview in the form is presentational only.

## 9. What I still verified by hand

- Ran `dotnet test` and confirmed every boundary test passes.
- Submitted the boundary cases through Swagger and checked the persisted rows in `lending.db`.
- Checked the statistics by hand against three known applications before trusting the endpoint.
- Confirmed the frontend degrades sensibly with the API stopped (it shows a message telling you to
  start the backend, rather than an empty screen).
