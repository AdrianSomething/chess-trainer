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

A successful CI run would not prove puzzle quality, legal compliance or all interactive UI behavior. Preserve manual fresh-install and accessibility checks.
