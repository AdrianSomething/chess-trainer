# Working in this repository

Start with docs/ARCHITECTURE.md. Maintained code is in src/ChessOpeningTrainer. Keep one responsibility per file; reuse BoardControl, BoardPresentation and AnalysisPresenter.

Domain stays UI/process-free. UCI belongs in Infrastructure/Engine, persistence in Infrastructure/Storage. Generators do not depend on Controls. UI colors use UiPalette; board colors use Skins. Preserve serialized fields and backward-compatible defaults.

Puzzle views register in Features/Puzzles/Tabs; branch actions in Features/Branches/Actions. Do not add unrelated rewrites or global feature-state singletons.

Use isolated CHESS_TRAINER_DATA directories for checks. Never copy personal profiles, PGNs, credentials or build output into source. Tests/CI export is deferred; see docs/CI_ASSESSMENT.md.

Do not upload, create releases, change repository settings or make legal guarantees without explicit authorization. Read THIRD_PARTY_NOTICES.md before modifying assets or packaging.
