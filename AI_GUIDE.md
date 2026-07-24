# AI Guide - ActionFit Time Server

## Package Identity

- Package ID: `com.actionfit.time.server`
- Display name: ActionFit Time Server
- Repository: `https://github.com/ActionFit-Editor/Time_Server.git`
- Repository visibility: Public
- Current package version at generation time: `1.0.5`
- Unity version: `6000.2`

## Purpose

ActionFit Time Server converts a fresh, non-cached HTTPS `Date` observation into an in-memory Unix-millisecond baseline and advances trusted UTC with a monotonic elapsed-time source. Its separate Unity settings assembly owns `TimeServerSettingsSO` and resolves device/server clock selection, explicit calendar mode, and signed day boundaries into `ActionFit.Time.ConfiguredGameTime`.

It does not own endpoint configuration, retries, startup UI, application lifecycle callbacks, gameplay input blocking, persisted developer offsets, daily-rollover side effects, event rewards, Firebase, or offline startup.

## Runtime Architecture

- `IMonotonicTimeSource` exposes elapsed milliseconds without a wall-clock contract.
- `StopwatchMonotonicTimeSource` is the runtime monotonic source.
- `ServerSessionClock` stores `serverUnixMsAtSync` and `monotonicMsAtSync` as integers.
- Current trusted time is `serverUnixMsAtSync + monotonicElapsedMs`.
- `ServerSessionClock.UtcNow` converts through `DateTimeOffset.FromUnixTimeMilliseconds(...).UtcDateTime` only at the `IClock` boundary.
- `Pause()` freezes the last trusted value until a fresh synchronization succeeds.
- `ServerTimeSynchronizer` requests one cache-bypassed observation and has no hidden retry policy.
- A valid observation requires HTTPS success, a parsed UTC `Date`, a valid absent-or-zero `Age`, and non-negative round-trip duration.
- The engine-neutral `com.actionfit.time.server` assembly retains the synchronization core and has no Unity engine reference.
- The Unity-dependent `com.actionfit.time.server.Unity` assembly owns `TimeServerSettingsSO` and depends on `com.actionfit.time` plus `com.actionfit.sosingleton`.
- The Editor assembly owns the settings inspector, safe bootstrap, and `Tools/Package/ActionFit Time Server/Setting SO` menu.

## Settings Contract

- `TimeServerCalendarMode.Utc = 0` and `TimeServerCalendarMode.DeviceLocal = 1` are serialized compatibility values.
- `DayBoundaryOffsetHours` supports `-23..23`; the serialized backing key remains `additionalOffsetHours`, and obsolete `AdditionalOffsetHours` remains as a source-compatibility read.
- `UseServerTime=false` selects the provided device clock, `TimeZoneInfo.Local`, and zero boundary, ignoring stored server-calendar details.
- `UseServerTime=true` selects the provided server clock, the configured UTC or device-local calendar, and the signed boundary in both calendar modes.
- The resulting calculation is `Now = selectedCalendarNow - DayBoundaryOffset`, `Today = Now.Date`, and `UtcNow = selected IClock.UtcNow`.
- Runtime singleton registration resolves `Assets/_Data/_TimeServer/Resources/SO/TimeServerSettingsSO.asset`. Canonical, declared legacy, or unique existing assets are reused; duplicate, invalid, or occupied states block creation.

## Safety And Integration Rules

- Do not add `DateTime.Now`, `DateTime.UtcNow`, `SystemClock.Instance.UtcNow`, PlayerPrefs, EditorPrefs, a database, or Firebase to the server-mode clock path.
- Do not persist the synchronized baseline across process restarts.
- Keep retry count, delay, blocking UI, and application lifecycle ownership in the consuming project.
- Propagate cancellation and input validation failures.
- Do not infer local, country, or business time zones from the HTTPS response. Consumers must convert trusted UTC with an explicit `TimeZoneInfo`.
- Do not move project endpoints, retry orchestration, database storage, DevTool controls, or gameplay side effects into the settings or synchronization assemblies.
- Keep developer offsets outside this package and compose them over the selected base clock through the existing project adapter.
- Publishing is manual. Do not create a repository, push, tag, or update a catalog without an explicit publish request.

## Project Router Registration

Requested router entry:

- `Packages/com.actionfit.time.server/AI_GUIDE.md` - ActionFit Time Server derives session-scoped trusted UTC and owns the Unity settings surface that resolves device/server clocks, calendar modes, and signed day boundaries.

## Validation

- Run `com.actionfit.time.server.Editor.Tests` for Unix-millisecond arithmetic, UTC conversion, half-round-trip anchoring, pause freeze, re-synchronization, and stale observation rejection.
- Include settings tests for device mode, server UTC/local mode, signed boundaries, exact UTC `0`/`-9` rollover, serialized-key compatibility, canonical/unique reuse, missing creation, and duplicate blocking.
- Run package contract validation for `com.actionfit.time.server`.
- Compile the consuming project after changing `IClock`, Connectivity observation, or adapter integration.
- Use fake observations and monotonic sources in automated tests. Never contact a public endpoint from unit tests.

## Package Tools Menu

- Unity menu root: `Tools/Package/ActionFit Time Server/`.
- `Setting SO` resolves and focuses the registered settings asset without moving or overwriting an existing valid asset.
- `README` opens the installed package README.

## Release Notes

- This package is Public; its direct ActionFit Time and Connectivity dependencies are also Public.
- Before reusing a version, check remote tags. Published tags are immutable.
- Keep `package.json`, README, this guide, PackageInfo, tests, and dependency versions aligned.
- `1.0.5` is the prepared release candidate. Publication, tags, and catalog mutation remain manual.
