# AI log

Tool used: **Claude (Anthropic)**, for architecture discussion, edge-case generation, code review and documentation. It was used as a pair programmer throughout — it drafted, I specified, questioned and verified. Every decision below was checked against the brief before it went into the repo.

> Note to self before submitting: swap in anything here that doesn't match what I actually did, and add any prompts from my own session that aren't listed. This log should be a record, not a template.

---

## 1. Clarifying the brief before writing code

**Prompt.** "Here is the lending brief. Before any code, list every requirement that is ambiguous or where two reasonable engineers would implement different behaviour, and tell me what the options are."

**What came back (the useful parts).** Six ambiguities, four of which mattered:

- Does "mean LTV across all applications" include declined applications?
- Does "total value of loans written" include declined applications?
- Is an LTV of exactly 60% inside the "< 60%" band or the "< 80%" band?
- Is a £1,000,000 loan "≥ £1 million" (large-loan rules) or under the threshold?

**My decision.** I read each literally and documented it in the README's Assumptions section rather than silently picking one. The one I spent longest on was LTV exactly 60%: the large-loan rule says "60% or less" (inclusive), while the small-loan ladder says "< 60%" (exclusive). Those are genuinely different comparisons in the same brief, so I implemented them differently and wrote a test for each — the large-loan test expects 60% to pass, the ladder test expects 60% to require a score of 800. I'd confirm this with the business in a real project rather than assume.

## 2. Architecture

**Prompt.** "Propose a structure for this that keeps the lending rules independently testable, with no rule logic in the controller or the frontend. Keep it small — this is a short assessment, not a platform. Tell me what you'd deliberately leave out."

**Outcome.** Controller → application service → rules engine, with the rules engine a pure class that takes three primitives and returns a decision. The first draft had the rules engine taking the EF entity and writing to the database itself. I rejected that: it would have made the rules untestable without a database, which is exactly what the brief grades on. Splitting the pure decision logic from the persistence/orchestration layer came out of that pushback, and the statistics aggregation was split out for the same reason.

I also asked what to leave out and held to that list: no repository layer over EF, no CQRS, no AutoMapper, no Docker, no auth. EF Core already is the repository and unit of work at this scale.

## 3. A real bug found by reviewing AI output: rounding before deciding

The first generated version of the rules engine did this:

```csharp
var ltv = Math.Round(loanAmount / assetValue * 100, 2);
if (ltv >= 90) return Declined;
```

I asked: "what happens at a loan of £899,999 against an asset of £1,000,000?"

True LTV is 89.9999%, which is under 90 and should be assessed in the 80–90 band. Rounded to 2dp it becomes 90.00 and is wrongly **declined**. That's a real financial difference caused by a display concern leaking into a business decision.

**Fix.** The engine decides on the unrounded `decimal` and rounds only the value returned for display and storage. There's a dedicated regression test for this case, and it fails against the original code.

## 4. Independent verification of the rules

I didn't want the tests and the implementation to share a single misreading of the brief, so I wrote a second implementation of the rules straight from the brief's wording, in a throwaway script, and ran both across every combination of 15 boundary-relevant loan amounts, 6 asset values and 11 credit scores (990 cases). Zero mismatches. That's a cross-check, not a substitute for the test suite — it caught nothing, which was the point of running it.

## 5. Correction: floating-point arithmetic in my own test data

My first pass at the boundary test cases derived the asset value from a target LTV:

```csharp
var assetValue = loan / ltv * 100m;   // then assert on the recomputed LTV
```

Reviewing this, the round-trip can land a fraction below the target — a "90%" case could evaluate as 89.999…% and pass when it should decline, meaning the test would assert the wrong thing while still going green. I rewrote the affected theories to fix the asset value (e.g. £1,000,000 / £2,000,000) and vary the loan amount instead, so every LTV in the test data is exact and the boundary being tested is unambiguous.

## 6. Correction: statistics and SQLite

The generated statistics query used an `AverageAsync` call directly on the database. SQLite has no native decimal type — EF Core maps `decimal` to TEXT — so server-side `SUM`/`AVG` on these columns is either unsupported or silently lossy. I changed it to materialise the rows and aggregate in C# with exact decimal arithmetic, and documented the scaling limitation in the README rather than pretending it isn't one.

## 7. Correction: invalid input is not a decline

An early draft returned "Declined" for a zero asset value. I disagreed: an application that can't have its LTV calculated hasn't been underwritten, and recording it as a decline would corrupt both the decline count and the mean LTV. Invalid input is now a 400 with a field-level message; the engine also throws if called directly with impossible values, as a safety net for any future non-HTTP caller.

## 8. Frontend

**Prompt.** "Design the UI around what an underwriter actually needs to see. Don't give me a generic dashboard of four identical cards."

The interesting output was a visual ruler showing where an application's LTV sits against the 60/80/90 thresholds, which makes the reason for a decision visible rather than just stated in text. I kept that and deliberately kept everything else plain.

I asked for client-side validation but was explicit that the backend stays authoritative — the frontend validation exists so the user gets instant feedback, and the API re-validates everything regardless. Rules living in React would undermine the separation-of-concerns criterion in the brief, so the frontend never computes a decision itself; any LTV shown before submission is presentational only.

## 9. What I still verified by hand

- Ran the full test suite and confirmed every boundary case passes.
- Submitted the boundary cases listed in the README through Swagger and checked the persisted rows in the database.
- Checked the statistics endpoint by hand against three known applications before trusting it.
- Confirmed the frontend degrades sensibly with the API stopped — it shows a message telling you to start the backend, rather than an empty or broken screen.
