# Continuous integration

## Enabled for 0.26.2-beta

The previous keep-disabled decision was superseded by the explicit request for
0.26.2. `.github/workflows/ci.yml` runs on pushes to `main` and `dev/**`, pull
requests, and manual dispatch. It builds the Windows application and runs the
curated synthetic checks in `tests/Public`, grouped into rules/codec and
storage/graph steps. A thousand assertions need not mean a thousand jobs.

The job uses the SDK selected by `global.json`, commit-pinned official checkout
and setup-dotnet actions, read-only repository permissions, no persisted checkout
credentials, a 15-minute timeout, and cancellation of superseded runs. It does
not use `pull_request_target`. NuGet/SDK restoration is the only dependency
download; there are no Lichess archive or Stockfish downloads in these checks.
No release is automatically created and no test profiles are uploaded.

The source publication allowlist includes only `tests/Public` and this specific
workflow. The larger existing local harness and private fixtures are not copied.

## Test organization and future expansion

| Group | Current execution | Suitable next step |
| --- | --- | --- |
| Rules and move codec | Every CI run, one fast process | More perft and rule fixtures |
| Storage and graph projection | Every CI run, synthetic isolated profiles | Fake-HTTP and old-format fixtures after separate publication review |
| WinForms smoke tests | Local private harness | Small off-screen construction checks, separate from human visual acceptance |
| Engine and puzzle generation | Local opt-in suite | Manual or scheduled bounded runs with a pinned engine |
| Benchmarks | Local bounded corpora | Report size/latency trends; avoid brittle wall-clock gates on shared runners |
| Release packaging/privacy | Local sanitized-source build and audit | An explicit release gate, not automatic publication |

The existing local harness includes more than 1,000 regression checks and over
22,000 dataset assertions, mostly cheap codec combinations. Test cost and fixture
privacy matter more than the count. Keep unit/integration checks fast enough for
every pull request, while expensive engine searches and large archives remain
opt-in. No successful CI run proves puzzle quality, legal compliance or complete
interactive behavior across themes, DPI settings and Windows versions.

Workflow presence does not override repository-level Actions permissions. Check
the repository's Actions page for the result of the first development-branch run.
