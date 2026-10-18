namespace ChessOpeningTrainer;
public sealed class DepthSection
{
    public int EndDepth { get; set; } = 10;
    public int ComplexThreshold { get; set; } = 4;
    public int RetainBest { get; set; } = 0;
    public double RetainMinEval { get; set; } = 0;
    public double EasyThreshold { get; set; } = -2;
    public int SearchDepth { get; set; } = 14;
    public double MaxReplyLoss { get; set; } = 2;
    public double CompressionLoss { get; set; } = .1;
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public double LearnerVariationLoss { get; set; }
}
