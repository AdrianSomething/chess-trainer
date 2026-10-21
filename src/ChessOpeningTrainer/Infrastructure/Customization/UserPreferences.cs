using System.Text.Json;

namespace ChessOpeningTrainer;
public sealed class UserPreferences
{
    public bool GeneratorRestEnabled { get; set; }
    public int GeneratorWorkMinutes { get; set; } = 60;
    public int GeneratorRestMinutes { get; set; } = 5;
    public GameAnalysisOptions GameAnalysis { get; set; } = new();
    public string Language { get; set; } = "en";
    public bool DebugInformation { get; set; }
    public bool UseCustomIcons { get; set; } = true;
    public string? LastLearningOpening { get; set; }
    public HashSet<string> BookmarkedOpenings { get; set; } = [];
    public int EvaluationWorkers { get; set; } = 3;
    public int TacticStockPerLane { get; set; } = 10;
    public int TacticGenerationWorkers { get; set; } = 2;
    public int TacticGenerationThreads { get; set; } = 1;
    public int QuickEvaluationDepth { get; set; } = 4;
    public int LightEvaluationDepth { get; set; } = 12;
    public int HeavyEvaluationDepth { get; set; } = 26;
    public string Skin { get; set; } = "classic";
    public string SoundPack { get; set; } =
#if PUBLIC_RELEASE && !PIXABAY_SOUNDS
        "legacy";
#else
        "user";
#endif
    public int Volume { get; set; } = 70;
    public string UiTheme { get; set; } = "classic";
    public int AnimationPercent { get; set; } = 100;
    public int ArrowThicknessPercent { get; set; } = 100;
    public int ArrowHeadPercent { get; set; } = 100;
    public string ArrowColor { get; set; } = "#ED8523";
    public string SelectionColor { get; set; } = "#F6F669";
    public string SquareMarkColor { get; set; } = "#F58E29";
    public bool BranchLayoutBeta { get; set; }
    // V18 starts with improved placement, independently of V17's saved beta opt-in.
    // Keep the older serialized field above so existing settings remain readable.
    public bool BranchLegacyPositioning { get; set; }
    public bool BranchGlobalRefinementBeta { get; set; }
    public double BranchColumnSpacing { get; set; } = 1;
    public bool VisionIndirectDefenders { get; set; }
    public int VisionMemoryDifficulty { get; set; } = 1;
    public int VisionGenerationWorkers { get; set; } = 2;
    public int VisionGenerationThreads { get; set; } = 1;
    public bool VisionStatisticsDemo { get; set; }
    public bool TutorialOffered { get; set; }
    public int HeartLossAnimation { get; set; }
    public bool LocalContinuousAnalysis { get; set; }
}
