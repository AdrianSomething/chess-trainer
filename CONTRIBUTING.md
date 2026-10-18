# Contributing

Read docs/DEVELOPMENT.md and docs/ARCHITECTURE.md. Search the responsible feature folder first. Keep one responsibility per file; reuse BoardControl, BoardPresentation and AnalysisPresenter.

Preserve saved JSON field names, existing defaults and user data. Domain code must not depend on Windows controls or process management. UI colors come from UiPalette and board colors from Skins. Avoid broad rewrites in focused bug fixes.

## Reports and changes

Describe expected/actual behavior, application and Windows versions, and minimal reproduction steps. Use synthetic PGNs and fresh profiles. Never attach credentials, private archives or unredacted personal screenshots.

Explain compatibility implications and verification, including what was not tested. Public documentation is English; German UI strings and translations are intentional.

The public regression harness is not yet exported. Include reproducible manual checks and focused examples; do not claim a full suite run you could not perform. See docs/CI_ASSESSMENT.md.

## Licensing and AI assistance

Submit only material you may contribute under the project license. Preserve notices and document asset provenance. Do not copy database dumps, paid assets or material of unclear provenance into the project.

Disclose material AI assistance and human verification when relevant. No particular model or AI tool is required. The maintainer's retrospective development record is in docs/ai-development/PROMPTS.md. Do not publish private prompts or credentials.

## Conduct

Be respectful, specific and constructive. Discuss behavior and evidence, not personal characteristics. Follow SECURITY.md for sensitive reports.
