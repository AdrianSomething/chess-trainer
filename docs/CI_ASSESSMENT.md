# Future GitHub Actions assessment — no workflow enabled

The private project has a substantial rules/UI/engine regression harness. Exporting it is deferred: this preparation intentionally adds neither the tests directory nor a GitHub Actions workflow.

## Recommended staged adoption

| Stage | Benefit | Constraints |
| --- | --- | --- |
| Build + pure chess rules on pull requests | Fast syntax/rule regressions; repeatable fresh environment | Use Windows and a pinned .NET SDK; synthetic fixtures only. |
| Focused storage and serialization tests | Protect saved-data compatibility | Temporary per-test profiles; no real PGNs or shared AppData. |
| WinForms smoke checks | Catch clipping, lifecycle and rendering regressions | Hosted Windows runners are not a full interactive desktop; separate off-screen checks from human UI acceptance. |
| Engine/generator checks | Catch UCI and training-quality regressions | Bound workers/time, pin engine/network, allow justified search variation; expensive runs belong in opt-in or scheduled jobs. |
| Release packaging checks | Verify licenses, source companion, hashes and private-data exclusion | Run before publishing artifacts; avoid uploading test profiles/logs wholesale. |

A thousand checks need not mean a thousand CI jobs. Group by responsibility and runtime; measure first. Cache verified dependencies, not user data. Keep credentials out of untrusted pull-request jobs and use minimum token permissions. Pin action versions/commits when a workflow is eventually designed.

## Dataset beta: proposed test groups

The local 0.26 harness adds over 22,000 assertions, largely exhaustive move-codec combinations. These are cheap iterations within one process, not a reason to create thousands of jobs or omit coverage. The existing rules, smoke and engine suites produced 1,168 passing checks in the local regression run; the tutorial lifecycle suite separately exercises all 48 cards. Counts are observations of this revision, not coverage percentages.

Suggested public layout when test export is approved:

| Folder / CI group | Pull requests | Scheduled / manual |
| --- | --- | --- |
| `tests/Unit/Chess` and `tests/Unit/Datasets` | All rules, move codec, filters, position identity and counts | Same |
| `tests/Integration/Storage` | Synthetic PGN/ZIP/Zstandard fixtures, pruning, cancellation and old-index preservation | Larger bounded synthetic corpora |
| `tests/Integration/Downloads` | Fake HTTP: resume, ignored Range, checksum failures, interrupted finalization | Explicit live connectivity check only; never a monthly archive on every run |
| `tests/Smoke/Windows` | Form construction and selected lifecycle checks in isolated profiles | Full tutorial and broader UI smoke suite |
| `tests/Engine` | Small deterministic protocol checks, if time permits | Full pinned-engine generation/regression run with concurrency limits |
| `tests/Benchmarks` | No fragile wall-clock assertions on shared runners | Codec size, peak memory, lookup latency and branch-layout scaling; report trends |

Start with two or three jobs grouped by execution requirements, not feature count. Pure rules/storage may eventually run cross-platform, but the current assembly targets WinForms, so Windows is the honest initial runner choice. Restore/build once per job; cache packages by lock/project inputs. Use timeouts and cancellation for background workers. Upload only selected synthetic failure artifacts, never copied profiles, game libraries or downloaded archives. A release gate should include the sanitized-source build, publication audit and matching-source package checks.

No Actions workflow or private tests are published as part of 0.26.0-beta. The one authorized live Lichess download was a local manual check; normal dataset tests inject fake HTTP and do not access the network. A full multi-gigabyte archive test remains an opt-in local benchmark.

A successful CI run would not prove puzzle quality, legal compliance or all interactive UI behavior. Preserve manual fresh-install and accessibility checks.
