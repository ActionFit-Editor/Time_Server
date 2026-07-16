# AI Guide - ActionFit Time Server

## Package Identity

- Package ID: `com.actionfit.time.server`
- Display name: ActionFit Time Server
- Repository: `https://github.com/ActionFit-Editor/Time_Server.git`
- Repository visibility: Public
- Current package version at generation time: `1.0.1`
- Unity version: `6000.2`

## Purpose

ActionFit Time Server converts a fresh, non-cached HTTPS `Date` observation into an in-memory Unix-millisecond baseline and advances trusted UTC with a monotonic elapsed-time source. It implements the existing `ActionFit.Time.IClock` compatibility contract without reading the device wall clock after server mode is selected.

It does not own endpoint configuration, retries, startup UI, application lifecycle callbacks, gameplay input blocking, persisted developer offsets, time-zone policy, daily rollover, event rewards, Firebase, or offline startup.

## Runtime Architecture

- `IMonotonicTimeSource` exposes elapsed milliseconds without a wall-clock contract.
- `StopwatchMonotonicTimeSource` is the runtime monotonic source.
- `ServerSessionClock` stores `serverUnixMsAtSync` and `monotonicMsAtSync` as integers.
- Current trusted time is `serverUnixMsAtSync + monotonicElapsedMs`.
- `ServerSessionClock.UtcNow` converts through `DateTimeOffset.FromUnixTimeMilliseconds(...).UtcDateTime` only at the `IClock` boundary.
- `Pause()` freezes the last trusted value until a fresh synchronization succeeds.
- `ServerTimeSynchronizer` requests one cache-bypassed observation and has no hidden retry policy.
- A valid observation requires HTTPS success, a parsed UTC `Date`, a valid absent-or-zero `Age`, and non-negative round-trip duration.

## Safety And Integration Rules

- Do not add `DateTime.Now`, `DateTime.UtcNow`, `SystemClock.Instance.UtcNow`, PlayerPrefs, EditorPrefs, a database, or Firebase to the server-mode clock path.
- Do not persist the synchronized baseline across process restarts.
- Keep retry count, delay, blocking UI, and application lifecycle ownership in the consuming project.
- Propagate cancellation and input validation failures.
- Do not infer local, country, or business time zones from the HTTPS response. Consumers must convert trusted UTC with an explicit `TimeZoneInfo`.
- Keep developer offsets outside this package and compose them over the selected base clock through the existing project adapter.
- Publishing is manual. Do not create a repository, push, tag, or update a catalog without an explicit publish request.

## Project Router Registration

Requested router entry:

- `Packages/com.actionfit.time.server/AI_GUIDE.md` - ActionFit Time Server derives session-scoped trusted UTC from fresh HTTPS Date observations and monotonic elapsed time without device wall-clock fallback.

## Validation

- Run `com.actionfit.time.server.Editor.Tests` for Unix-millisecond arithmetic, UTC conversion, half-round-trip anchoring, pause freeze, re-synchronization, and stale observation rejection.
- Run package contract validation for `com.actionfit.time.server`.
- Compile the consuming project after changing `IClock`, Connectivity observation, or adapter integration.
- Use fake observations and monotonic sources in automated tests. Never contact a public endpoint from unit tests.

## Package Tools Menu

- Unity menu root: `Tools/Package/ActionFit Time Server/`.
- `README` opens the installed package README.

## Release Notes

- This package is Public; its direct ActionFit Time and Connectivity dependencies are also Public.
- Before reusing a version, check remote tags. Published tags are immutable.
- Keep `package.json`, README, this guide, PackageInfo, tests, and dependency versions aligned.
